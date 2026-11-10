using System;

namespace Swg.Client.Core.Assets.Formats.Cadf
{
    public sealed class CadfWearable
    {
        public string TemplatePath { get; set; } = "";
        public byte[] Customization { get; set; } = Array.Empty<byte>();
        public uint Arrangement { get; set; }
    }
}
