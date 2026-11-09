using System;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DataColumnAttribute : Attribute
    {
        public string Name { get; }
        public Type Converter { get; set; }
        /// <summary>A missing optional column leaves the model initializer intact unless DefaultValue is supplied.</summary>
        public bool Optional { get; set; }
        public bool HasDefaultValue { get; private set; }

        object defaultValue;
        /// <summary>A raw cell fallback for a missing optional column; passed through Converter when one is specified.</summary>
        public object DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
                HasDefaultValue = true;
            }
        }

        public DataColumnAttribute(string name = null) => Name = name;
    }
}
