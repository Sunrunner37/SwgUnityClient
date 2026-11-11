namespace Swg.Client.Core.World.Terrain
{
    /// <summary>A weighted appearance or radial shader from the terrain's FGRP/RGRP families.</summary>
    public sealed class TerrainFloraItem
    {
        public string Path;
        public float Weight;
        public bool Radial;
        public bool Floats;
        public bool AlignToTerrain;
        public bool Crossed;
        public float Width = 1;
        public float Height = 1;
        public float Distance;
        public float MinimumScale = 1;
        public float MaximumScale = 1;
        public float SwayDisplacement;
        public float SwayPeriod;
    }
}
