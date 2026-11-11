using System;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    public abstract class Boundary
    {
        public int FeatherType { get; }
        public float Feather { get; }

        protected readonly float feather;
        protected Boundary(int featherType, float feather)
        {
            FeatherType = featherType;
            Feather = feather;
            this.feather = Math.Max(0, feather);
        }

        public float Sample(float x, float z) => TerrainFeather.Apply(SampleWeight(x, z), FeatherType);
        protected abstract float SampleWeight(float x, float z);
    }
}
