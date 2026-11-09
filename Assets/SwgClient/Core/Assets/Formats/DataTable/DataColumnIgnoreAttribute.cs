using System;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DataColumnIgnoreAttribute : Attribute
    {
    }
}
