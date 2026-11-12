using System;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World.Streaming
{
    [CreateAssetMenu(menuName = "SWG/World Streaming Settings")]
    public sealed class WorldStreamingSettings : ScriptableObject
    {
        [Min(1)]
        public float viewRadius = 512;
        [Min(1)]
        public float terrainRadius = 2048;
        [Min(1)]
        public float terrainViewDistance = 1800;
        [Min(1)]
        public float terrainMidRadius = 512;
        [Min(1)]
        public float terrainFarSpacing = 32;
        [Min(1)]
        public float middleFloraDistance = 160;
        [Min(1)]
        public float farFloraDistance = 320;
        [Min(1)]
        public float staticFloraDistance = 384;
        [Min(1)]
        public float floraFadeDistance = 24;
        [Min(1)]
        public float terrainTileSize = 256;
        [Min(.01f)]
        public float terrainSpacing = 8;
        [Min(1)]
        public float terrainDetailRadius = 128;
        [Min(1)]
        public float refreshDistance = 32;
        [Min(1)]
        public int uploadsPerFrame = 8;
        [Min(.1f)]
        public float uploadBudgetMilliseconds = 2;
        [Min(.1f)]
        public float initialUploadBudgetMilliseconds = 8;
        [Min(1)]
        public float floraRadius = 512;
        [Min(1)]
        public float floraTileSize = 64;
        [Min(1)]
        public float nearFloraDistance = 80; // Full-density grass ends here; coarser cards continue into the distance.
        [Min(1)]
        public float particleFloraDistance = 64;
        public (float Minimum, float Maximum) GetFloraRange(TerrainFloraDistribution distribution, TerrainFloraKind kind, bool particles = false)
        {
            if (distribution.MaximumDistance <= distribution.MinimumDistance)
                return (distribution.MinimumDistance, distribution.MinimumDistance);
            float maximum = kind == TerrainFloraKind.DynamicNear ? farFloraDistance : kind == TerrainFloraKind.DynamicFar ? Math.Min(farFloraDistance, distribution.MaximumDistance) : staticFloraDistance;
            maximum = Mathf.Min(floraRadius, maximum);
            if (particles)
                maximum = Mathf.Min(particleFloraDistance, maximum);
            return (distribution.MinimumDistance, maximum);
        }

        // Distance budgets for the three nested grass subsets.
        public float GrassDistance(int detail) => detail == 0 ? nearFloraDistance : detail == 1 ? middleFloraDistance : farFloraDistance;
        public void Validate()
        {
            if (!Positive(viewRadius) || !Positive(terrainRadius) || !Positive(terrainTileSize) || !Positive(terrainSpacing) || !Positive(terrainDetailRadius) || !Positive(refreshDistance) || uploadsPerFrame < 1 || !Positive(uploadBudgetMilliseconds) || !Positive(initialUploadBudgetMilliseconds))
                throw new InvalidOperationException("World streaming settings must be positive finite values.");
            if (!Positive(floraRadius) || !Positive(floraTileSize) || !Positive(nearFloraDistance) || !Positive(particleFloraDistance))
                throw new InvalidOperationException("Flora streaming distances must be positive finite values.");
            if (!Positive(terrainViewDistance) || !Positive(terrainMidRadius) || !Positive(terrainFarSpacing) || terrainMidRadius < terrainDetailRadius || terrainFarSpacing < terrainSpacing || Mathf.Abs(terrainTileSize / terrainFarSpacing - Mathf.Round(terrainTileSize / terrainFarSpacing)) > .001f)
                throw new InvalidOperationException("Terrain LOD radii and spacing must increase with distance, with spacing dividing the tile size.");
            if (!Positive(middleFloraDistance) || !Positive(farFloraDistance) || !Positive(staticFloraDistance) || !Positive(floraFadeDistance) || nearFloraDistance >= middleFloraDistance || middleFloraDistance >= farFloraDistance)
                throw new InvalidOperationException("Flora detail distances must increase with distance.");
            if (floraRadius < Math.Max(farFloraDistance, staticFloraDistance) + refreshDistance + 64 || terrainRadius < terrainViewDistance + refreshDistance + 64)
                throw new InvalidOperationException("Loaded terrain and flora must include a margin beyond their drawing distances.");
            if (terrainRadius < viewRadius || refreshDistance > terrainRadius - viewRadius)
                throw new InvalidOperationException("Terrain must extend beyond the view radius by at least the refresh distance.");
            float cells = terrainTileSize / terrainSpacing;
            if (cells > 512 || cells < 1 || Mathf.Abs(cells - Mathf.Round(cells)) > .001f)
                throw new InvalidOperationException("Terrain tile size must be a multiple of spacing, with at most 512 cells per edge.");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            try { Validate(); }
            catch (InvalidOperationException error) { Debug.LogWarning(error.Message, this); }
        }
#endif
        static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
