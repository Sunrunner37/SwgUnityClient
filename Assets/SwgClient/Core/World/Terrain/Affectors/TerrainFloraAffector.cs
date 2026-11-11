using System;
using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainFloraAffector
    {
        public static TerrainLayer.Affector Create(TrnFloraRule definition, WorldTerrain terrain)
        {
            string kind = definition.Kind;
            bool dynamic = kind == "AFDN" || kind == "AFDF";
            int family = definition.FamilyId;
            int operation = definition.Operation;
            bool removeAll = definition.RemoveAll;
            bool overrideDensity = definition.UseOverride;
            float density = definition.Density;
            if (!overrideDensity)
                density = terrain.Flora.Density(family, dynamic);
            void Apply(float x, float z, float weight, ref TerrainFloraSelection selection)
            {
                if (removeAll)
                {
                    selection = default;
                    return;
                }

                var random = new TerrainRandom(x, z);
                if (random.Next() > weight * density)
                    return;
                if (operation == 1)
                    selection = new TerrainFloraSelection(family, random.Next());
                else if (operation == 0 && selection.FamilyId == family)
                    selection = default;
            }

            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                switch (kind)
                {
                    case "AFSC":
                        Apply(x, z, weight, ref sample.StaticCollidable);
                        break;
                    case "AFSN":
                        Apply(x, z, weight, ref sample.StaticNonCollidable);
                        break;
                    case "AFDN":
                        Apply(x, z, weight, ref sample.DynamicNear);
                        break;
                    case "AFDF":
                        Apply(x, z, weight, ref sample.DynamicFar);
                        break;
                }
            };
        }
    }
}
