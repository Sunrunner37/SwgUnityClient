using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Models
{
    public sealed class ModelData
    {
        public string Key;
        public List<MeshPart> Parts;
        public bool[] AlphaCutout;
        public byte[][] VertexColors;
        public bool Character;
    }
}
