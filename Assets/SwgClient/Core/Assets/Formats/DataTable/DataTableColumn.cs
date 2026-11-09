using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    public sealed class DataTableColumn
    {
        public string Name { get; }
        public string TypeDefinition { get; }
        public Type ValueType { get; }

        readonly Func<ByteReader, object> read;
        DataTableColumn(string name, string definition, Type valueType, Func<ByteReader, object> read)
        {
            Name = name;
            TypeDefinition = definition;
            ValueType = valueType;
            this.read = read;
        }

        internal object Read(ByteReader reader) => read(reader);
        internal static DataTableColumn Parse(string name, string definition)
        {
            if (definition.Length == 0)
                throw new InvalidDataException("Missing data-table type for column " + name);
            // The first character defines storage; retain default/enum annotations for consumers.
            switch (definition[0])
            {
                case 's':
                    return new DataTableColumn(name, definition, typeof(string), r => r.CString());
                case 'f':
                    return new DataTableColumn(name, definition, typeof(float), r => BitConverter.ToSingle(r.Take(sizeof(float)), 0));
                case 'b':
                    return new DataTableColumn(name, definition, typeof(bool), r => r.U32() != 0);
                case 'h':
                    return new DataTableColumn(name, definition, typeof(uint), r => r.U32());
                case 'c':
                case 'p':
                case 'e':
                case 'z':
                case 'i':
                case 'I':
                    return new DataTableColumn(name, definition, typeof(int), r => unchecked((int)r.U32()));
                default:
                    throw new NotSupportedException("Unsupported data-table type '" + definition + "' for column " + name);
            }
        }
    }
}
