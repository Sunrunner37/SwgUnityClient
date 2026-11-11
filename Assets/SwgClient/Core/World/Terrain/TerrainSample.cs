using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Independent terrain-generator maps evaluated at an absolute SWG world position.</summary>
    public struct TerrainSample
    {
        public float Height;
        public int ShaderFamily;
        public Color32 Color;
        public int EnvironmentFamily;
        public TerrainFloraSelection StaticCollidable;
        public TerrainFloraSelection StaticNonCollidable;
        public TerrainFloraSelection DynamicNear;
        public TerrainFloraSelection DynamicFar;
        public static TerrainSample Empty => new TerrainSample
        {
            Color = new Color32(255, 255, 255, 255)
        };

        public void ClearFlora()
        {
            StaticCollidable = default;
            StaticNonCollidable = default;
            DynamicNear = default;
            DynamicFar = default;
        }
    }
}
