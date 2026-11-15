using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Swh
{
    /// <summary>SWH (Swoosh Appearance) defines the shader, RGB color, width and texture used for a motion ribbon.</summary>
    public sealed class SwhFile
    {
        public string Shader { get; private set; }
        public Color Color { get; private set; }
        public float Width { get; private set; }
        public static SwhFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "SWSH");
            var version = root.Children.Single(n => n.Tag == "FORM");
            var texture = new ByteReader(version.Children.Single(n => n.Kind == "PTEX").Chunk("0000"));
            var reader = new ByteReader(version.Chunk("0000"));
            return new SwhFile { Shader = texture.CString(), Color = new Color(reader.Float(), reader.Float(), reader.Float(), reader.Float()), Width = reader.Float() };
        }
    }
}
