namespace Swg.Client.Core.World.Terrain.Filters
{
    public abstract class TerrainFilter
    {
        public abstract float Sample(float x, float z, float height);
    }
}
