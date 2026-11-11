using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainShaderAffector
    {
        public static TerrainLayer.Affector Create(TrnShaderRule definition, WorldTerrain terrain)
        {
            int source = definition.SourceFamilyId;
            int destination = definition.FamilyId;
            bool useOverride = definition.UseOverride;
            float threshold = definition.Threshold;
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                float clamp = useOverride ? threshold : terrain.GetSurfaceDefinition(destination)?.FeatherClamp ?? 1;
                if ((source == -1 || sample.ShaderFamily == source) && weight >= clamp)
                    sample.ShaderFamily = destination;
            };
        }
    }
}
