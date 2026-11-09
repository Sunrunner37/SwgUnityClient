using System;
using System.Collections.Generic;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    public sealed class DataTableRow
    {
        readonly IReadOnlyList<DataTableColumn> columns;
        public IReadOnlyList<object> Values { get; }

        internal DataTableRow(IReadOnlyList<DataTableColumn> columns, object[] values)
        {
            this.columns = columns;
            Values = Array.AsReadOnly(values);
        }

        public T Get<T>(int columnIndex)
        {
            if (Values[columnIndex] is T value)
                return value;
            throw new InvalidCastException("Column '" + columns[columnIndex].Name + "' contains " + columns[columnIndex].ValueType.Name + ", not " + typeof(T).Name);
        }

        public T Get<T>(string columnName)
        {
            int index = -1;
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Name != columnName)
                    continue;
                if (index >= 0)
                    throw new InvalidOperationException("Ambiguous data-table column '" + columnName + "'. Use its index.");
                index = i;
            }

            if (index < 0)
                throw new KeyNotFoundException("Missing data-table column '" + columnName + "'.");
            return Get<T>(index);
        }
    }
}
