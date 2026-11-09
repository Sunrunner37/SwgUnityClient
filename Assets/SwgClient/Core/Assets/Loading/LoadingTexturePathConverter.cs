using System;
using Swg.Client.Core.Assets.Formats.DataTable;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Assets.Loading
{
    [Preserve]
    public sealed class LoadingTexturePathConverter : IDataTableValueConverter
    {
        public Type SourceType => typeof(string);
        public Type TargetType => typeof(string);

        [Preserve]
        public LoadingTexturePathConverter()
        {
        }

        public object Convert(object value)
        {
            string path = (string)value;
            if (string.IsNullOrWhiteSpace(path))
                return path;
            path = path.Replace('\\', '/');
            if (!path.StartsWith("texture/", StringComparison.OrdinalIgnoreCase))
                path = "texture/" + path;
            if (!path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                path += ".dds";
            return path;
        }
    }
}
