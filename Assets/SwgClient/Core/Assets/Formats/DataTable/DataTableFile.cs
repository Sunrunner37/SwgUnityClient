using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    /// <summary>
    /// DTII identifies SWG's compiled data tables inside an IFF container.
    /// COLS stores column names, TYPE stores storage/default definitions, and ROWS stores typed cells.
    /// This reader supports version 0001 and the storage types implemented by Core3.
    /// </summary>
    /// <seealso href="https://github.com/swgemu/Core3/blob/unstable/MMOCoreORB/src/templates/datatables/DataTableIff.cpp">Core3 data-table reader</seealso>
    public sealed class DataTableFile
    {
        public string Version { get; }
        public IReadOnlyList<DataTableColumn> Columns { get; }
        public IReadOnlyList<DataTableRow> Rows { get; }

        DataTableFile(string version, IReadOnlyList<DataTableColumn> columns, DataTableRow[] rows)
        {
            Version = version;
            Columns = columns;
            Rows = Array.AsReadOnly(rows);
        }

        public static DataTableFile Parse(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            var roots = IffNode.Parse(bytes);
            if (roots.Count != 1 || roots[0].Kind != "DTII")
                throw new InvalidDataException("Expected a DTII data-table form.");
            var versions = roots[0].Children.Where(n => n.Tag == "FORM").ToArray();
            if (versions.Length != 1)
                throw new InvalidDataException("Expected one DTII version form.");
            var version = versions[0];
            if (version.Kind != "0001")
                throw new NotSupportedException("Unsupported DTII version " + version.Kind);
            var names = Reader(version, "COLS");
            var types = Reader(version, "TYPE");
            var rows = Reader(version, "ROWS");
            int columnCount = checked((int)names.U32());
            if (columnCount == 0)
                throw new InvalidDataException("A data table must define columns.");
            if (columnCount > names.Remaining || columnCount > types.Remaining)
                throw new InvalidDataException("Truncated data-table columns.");
            var columns = new DataTableColumn[columnCount];
            for (int i = 0; i < columnCount; i++)
                columns[i] = DataTableColumn.Parse(names.CString(), types.CString());
            int rowCount = checked((int)rows.U32());
            // Every supported cell requires at least one byte. Reject impossible allocations before materializing rows.
            if ((long)rowCount * columnCount > rows.Remaining)
                throw new InvalidDataException("Truncated data-table cells.");
            var schema = Array.AsReadOnly(columns);
            var result = new DataTableRow[rowCount];
            for (int r = 0; r < rowCount; r++)
            {
                var values = new object[columnCount];
                for (int c = 0; c < columnCount; c++)
                    values[c] = columns[c].Read(rows);
                result[r] = new DataTableRow(schema, values);
            }

            if (names.Remaining != 0 || types.Remaining != 0 || rows.Remaining != 0)
                throw new InvalidDataException("Unexpected trailing data in DTII COLS, TYPE, or ROWS chunk.");
            return new DataTableFile(version.Kind, schema, result);
        }

        static ByteReader Reader(IffNode version, string tag)
        {
            var chunks = version.Children.Where(n => n.Tag == tag).ToArray();
            if (chunks.Length != 1)
                throw new InvalidDataException("Expected one data-table " + tag + " chunk.");
            return new ByteReader(chunks[0].Data);
        }
    }
}
