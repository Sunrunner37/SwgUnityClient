using System;
using Swg.Client.Core.World.Terrain.Boundaries;
using Swg.Client.Core.World.Terrain.Filters;
using System.Collections.Generic;

namespace Swg.Client.Core.World.Terrain
{
    internal sealed class TerrainLayer
    {
        internal delegate void Affector(float x, float z, float weight, ref TerrainSample sample);
        internal bool Enabled;
        internal bool InvertBoundaries;
        internal bool InvertFilters;
        internal readonly List<Boundary> Boundaries = new List<Boundary>();
        internal readonly List<TerrainFilter> Filters = new List<TerrainFilter>();
        internal readonly List<Affector> Affectors = new List<Affector>();
        internal readonly List<TerrainLayer> Children = new List<TerrainLayer>();
        internal void Apply(float x, float z, float parentWeight, ref TerrainSample sample)
        {
            if (!Enabled)
                return;
            float weight = Boundaries.Count == 0 ? 1 : 0;
            foreach (var boundary in Boundaries)
                weight = Math.Max(weight, boundary.Sample(x, z));
            if (InvertBoundaries)
                weight = 1 - weight;
            if (weight == 0)
                return;
            foreach (var filter in Filters)
                weight = Math.Min(weight, filter.Sample(x, z, sample.Height));
            if (InvertFilters)
                weight = 1 - weight;
            if (weight == 0)
                return;
            weight *= parentWeight;
            foreach (var affector in Affectors)
                affector(x, z, weight, ref sample);
            foreach (var child in Children)
                child.Apply(x, z, weight, ref sample);
        }
    }
}
