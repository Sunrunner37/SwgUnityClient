using Swg.Client.Core.Assets.Particles;
using Swg.Client.Core.Characters.Models;
using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Appearances
{
    public sealed class SkeletalAppearanceData
    {
        public readonly List<ParticleAppearance> Particles = new List<ParticleAppearance>();
        public readonly List<MeshPart> Parts = new List<MeshPart>();
        public readonly List<CachedMeshPartSource> Sources = new List<CachedMeshPartSource>();
    }
}
