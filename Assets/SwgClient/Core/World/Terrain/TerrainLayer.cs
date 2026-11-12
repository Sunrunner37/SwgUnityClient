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
        internal bool HasSurfaceChanges;
        internal int Index;
        internal Func<float, float, float> SampleNormalY;
        internal bool InvertBoundaries;
        internal bool InvertFilters;
        internal readonly List<Boundary> Boundaries = new List<Boundary>();
        internal readonly List<TerrainFilter> Filters = new List<TerrainFilter>();
        internal readonly List<Affector> Affectors = new List<Affector>();
        internal readonly List<Affector> SurfaceAffectors = new List<Affector>();
        internal readonly List<TerrainLayer> Children = new List<TerrainLayer>();
        internal void Apply(float x, float z, float parentWeight, ref TerrainSample sample, int endLayer = int.MaxValue, bool surfaceOnly = false)
        {
            if (!Enabled || Index >= endLayer || surfaceOnly && !HasSurfaceChanges)
                return;
            float weight = Boundaries.Count == 0 ? 1 : 0;
            foreach (var boundary in Boundaries)
                weight = Math.Max(weight, boundary.Sample(x, z));
            if (InvertBoundaries)
                weight = 1 - weight;
            if (weight == 0)
                return;
            float normalY = 1;
            bool sampledNormal = false;
            foreach (var filter in Filters)
            {
                if (filter.NeedsNormals && !sampledNormal)
                {
                    normalY = SampleNormalY(x, z);
                    sampledNormal = true;
                }
                weight = Math.Min(weight, filter.Sample(x, z, sample, normalY));
                if (weight == 0)
                    break;
            }
            if (InvertFilters)
                weight = 1 - weight;
            if (weight == 0)
                return;
            weight *= parentWeight;
            foreach (var affector in surfaceOnly ? SurfaceAffectors : Affectors)
                affector(x, z, weight, ref sample);
            foreach (var child in Children)
                child.Apply(x, z, weight, ref sample, endLayer, surfaceOnly);
        }
    }
}
