using System;
using System.Collections.Generic;

namespace Swg.Client.Core.World.Terrain
{
    internal sealed class TerrainLayer
    {
        internal delegate void Affector(float x, float z, float weight, ref float height, ref int shader);
        internal bool Enabled;
        internal bool InvertBoundaries;
        internal bool InvertFilters;
        internal readonly List<TerrainBoundary> Boundaries = new List<TerrainBoundary>();
        internal readonly List<Func<float, float, float, float>> Filters = new List<Func<float, float, float, float>>();
        internal readonly List<Affector> Affectors = new List<Affector>();
        internal readonly List<TerrainLayer> Children = new List<TerrainLayer>();
        internal void Apply(float x, float z, float parentWeight, ref float height, ref int shader)
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
                weight = Math.Min(weight, filter(x, z, height));
            if (InvertFilters)
                weight = 1 - weight;
            if (weight == 0)
                return;
            weight *= parentWeight;
            foreach (var affector in Affectors)
                affector(x, z, weight, ref height, ref shader);
            foreach (var child in Children)
                child.Apply(x, z, weight, ref height, ref shader);
        }
    }
}
