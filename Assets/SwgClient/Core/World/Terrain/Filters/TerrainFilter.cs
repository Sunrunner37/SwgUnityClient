namespace Swg.Client.Core.World.Terrain.Filters
{
    public abstract class TerrainFilter
    {
        public virtual bool NeedsNormals => false;
        public abstract float Sample(float x, float z, in TerrainSample sample, float normalY = 1);
    }
}
