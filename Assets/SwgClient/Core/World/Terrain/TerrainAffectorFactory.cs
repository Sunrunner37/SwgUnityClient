using System;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.World.Terrain.Affectors;

namespace Swg.Client.Core.World.Terrain
{
    internal sealed class TerrainAffectorFactory
    {
        readonly WorldTerrain terrain;
        readonly TreAssetLoader assets;
        public TerrainAffectorFactory(WorldTerrain terrain, TreAssetLoader assets)
        {
            this.terrain = terrain;
            this.assets = assets;
        }

        public TerrainLayer.Affector Create(TrnRule definition) => definition switch
        {
            TrnRoadRule road => new TerrainRoad(road.Road).Apply,
            TrnFloraRule flora => TerrainFloraAffector.Create(flora, terrain),
            TrnShaderRule shader => TerrainShaderAffector.Create(shader, terrain),
            TrnEnvironmentRule environment => TerrainEnvironmentAffector.Create(environment, terrain),
            TrnColorRampRule color => TerrainColorRampAffector.Create(color, terrain, assets),
            TrnTerraceRule terrace => TerrainTerraceAffector.Create(terrace),
            TrnHeightRule height => TerrainHeightAffector.Create(height, terrain),
            _ => throw new NotSupportedException("Terrain affector " + definition.Kind)};
    }
}
