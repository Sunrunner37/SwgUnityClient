using System;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    [CreateAssetMenu(menuName = "SWG/World Streaming Settings")]
    public sealed class WorldStreamingSettings : ScriptableObject
    {
        [Min(1)]
        public float viewRadius = 512;
        [Min(1)]
        public float terrainRadius = 768;
        [Min(1)]
        public float terrainTileSize = 256;
        [Min(.01f)]
        public float terrainSpacing = 8;
        [Min(1)]
        public float refreshDistance = 64;
        [Min(1)]
        public int uploadsPerFrame = 8;
        [Min(1)]
        public float floraRadius = 128;
        [Min(1)]
        public float floraTileSize = 64;
        [Min(1)]
        public float floraSpacing = 4;
        [Min(1)]
        public float nearFloraDistance = 64;
        public void Validate()
        {
            if (!Positive(viewRadius) || !Positive(terrainRadius) || !Positive(terrainTileSize) || !Positive(terrainSpacing) || !Positive(refreshDistance) || uploadsPerFrame < 1)
                throw new InvalidOperationException("World streaming settings must be positive finite values.");
            if (!Positive(floraRadius) || !Positive(floraTileSize) || !Positive(floraSpacing) || !Positive(nearFloraDistance) || floraSpacing > floraTileSize)
                throw new InvalidOperationException("Flora streaming distances must be positive and spacing must fit within a tile.");
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
