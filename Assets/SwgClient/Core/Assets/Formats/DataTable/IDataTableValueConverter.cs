using System;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    /// <summary>Converts a decoded cell value. A fresh converter is created per bound member for each Read call.</summary>
    public interface IDataTableValueConverter
    {
        Type SourceType { get; }

        Type TargetType { get; }

        object Convert(object value);
    }
}
