using Swg.Client.Core.Assets.Formats.DataTable;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Assets.Loading
{
    [Preserve]
    public sealed class LoadingCard
    {
        [Preserve, DataColumn("StringId")]
        public string Description { get; set; }

        [Preserve, DataColumn("Picture", Converter = typeof(LoadingTexturePathConverter))]
        public string PicturePath { get; set; }

        [Preserve]
        public string Title { get; set; }
    }
}
