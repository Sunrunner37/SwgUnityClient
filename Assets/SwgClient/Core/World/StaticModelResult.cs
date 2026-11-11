using UnityEngine;
using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.World
{
    public sealed class StaticModelResult
    {
        public readonly List<MeshPart> Parts = new List<MeshPart>();
        public readonly List<StaticMeshSource> Sources = new List<StaticMeshSource>();
        public readonly List<string> Warnings = new List<string>();
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
        public float Radius;
    }
}
