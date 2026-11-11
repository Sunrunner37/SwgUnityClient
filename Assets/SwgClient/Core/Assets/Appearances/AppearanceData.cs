using UnityEngine;
using System;
using Swg.Client.Core.Assets.Particles;
using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Appearances
{
    public sealed class AppearanceData
    {
        public string Key;
        public List<ParticleAppearance> Particles = new List<ParticleAppearance>();
        public List<MeshPart> Parts = new List<MeshPart>();
        public readonly List<StaticMeshSource> Sources = new List<StaticMeshSource>();
        public readonly List<string> Warnings = new List<string>();
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
        public float Radius;
        public void RecalculateBounds()
        {
            BoundsMin = Vector3.zero;
            BoundsMax = Vector3.zero;
            Radius = 0;
            bool first = true;
            foreach (var part in Parts)
                foreach (var p in part.Vertices)
                {
                    if (first)
                    {
                        BoundsMin = p;
                        BoundsMax = p;
                        first = false;
                    }

                    BoundsMin = Vector3.Min(BoundsMin, p);
                    BoundsMax = Vector3.Max(BoundsMax, p);
                    Radius = Math.Max(Radius, p.magnitude);
                }
        }
    }
}
