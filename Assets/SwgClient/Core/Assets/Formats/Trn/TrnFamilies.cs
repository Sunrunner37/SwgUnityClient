using System.Collections.Generic;
using Swg.Client.Core.World.Terrain.Boundaries;
using Swg.Client.Core.World.Terrain.Filters;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnShaderFamily
    {
        public int Id;
        public string Name;
        public string SurfaceProperties;
        public Color32 EditorColor;
        public float TileSize;
        public float FeatherClamp;
        public readonly List<TrnShaderChild> Children = new List<TrnShaderChild>();
    }

    public sealed class TrnShaderChild
    {
        public string Path;
        public float Weight;
    }

    public sealed class TrnEnvironmentFamily
    {
        public int Id;
        public string Name;
        public Color32 EditorColor;
        public float FeatherClamp;
    }

    public sealed class TrnLayer
    {
        public bool Enabled;
        public bool InvertBoundaries;
        public bool InvertFilters;
        public readonly List<TrnLayer> Children = new List<TrnLayer>();
        public readonly List<Boundary> Boundaries = new List<Boundary>();
        public readonly List<TrnRule> Affectors = new List<TrnRule>();
        public readonly List<TerrainFilter> Filters = new List<TerrainFilter>();
        public readonly List<string> UnsupportedRules = new List<string>();
    }
}
