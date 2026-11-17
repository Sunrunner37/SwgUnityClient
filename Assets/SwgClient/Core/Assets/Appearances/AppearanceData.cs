using Swg.Client.Core.Assets.Formats.Pob;
using Swg.Client.Core.Assets.Audio;
using UnityEngine;
using System;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Idtl;
using Swg.Client.Core.Assets.Particles;
using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Appearances
{
    public sealed class AppearanceData
    {
        public string Key;
        public readonly List<IdtlFile> RadarShapes = new List<IdtlFile>();
        // Transport flight bindings identify shuttles even when their template uses the ordinary NPC type.
        public bool IsPassengerTransport => Parts.Any(part => part.Skinning?.Skeleton?.IsPassengerTransport == true);
        public readonly List<SoundAppearance> Sounds = new List<SoundAppearance>();
        public bool HasContent => Parts.Count != 0 || Particles.Count != 0 || Sounds.Count != 0;
        public PortalLayout PortalLayout;
        public readonly List<DoorAppearance> Doors = new List<DoorAppearance>();
        public readonly Dictionary<string, Matrix4x4> Hardpoints = new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);
        public List<ParticleAppearance> Particles = new List<ParticleAppearance>();
        public RiderSeatData? RiderSeat;
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
