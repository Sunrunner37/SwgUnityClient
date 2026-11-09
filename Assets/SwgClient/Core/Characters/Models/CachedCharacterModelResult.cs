using System.Collections.Generic;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Models
{
    public sealed class CachedCharacterModelResult
    {
        public readonly List<MeshPart> Parts = new List<MeshPart>();
        public readonly List<CachedMeshPartSource> Sources = new List<CachedMeshPartSource>();
        public readonly List<string> Warnings = new List<string>();
    }
}
