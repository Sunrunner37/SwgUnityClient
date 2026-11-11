using Swg.Client.Core.Assets.Formats.Dds;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>A native SGRP shader family and its representative highest-weight shader variant.</summary>
    public sealed class TerrainSurface
    {
        public int FamilyId { get; internal set; }
        public string Name { get; internal set; }
        public string ShaderPath { get; internal set; }
        public string TexturePath { get; internal set; }
        public float TileSize { get; internal set; }
        public TextureData Diffuse { get; internal set; }
    }
}
