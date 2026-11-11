using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainEnvironmentAffector
    {
        public static TerrainLayer.Affector Create(TrnEnvironmentRule definition, WorldTerrain terrain)
        {
            int id = definition.FamilyId;
            bool useOverride = definition.UseOverride;
            float threshold = definition.Threshold;
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                if (weight >= (useOverride ? threshold : terrain.Environments.FeatherClamp(id)))
                    sample.EnvironmentFamily = id;
            };
        }
    }
}
