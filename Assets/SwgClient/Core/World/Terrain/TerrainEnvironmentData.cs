using Swg.Client.Core.Assets.Formats.Dds;
using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class TerrainEnvironmentData
    {
        public string Name;
        public TextureData ColorRamp;
        public TextureData SkyGradient;
        public CloudLayer BottomClouds;
        public CloudLayer TopClouds;
        public sealed class CloudLayer
        {
            public TextureData Diffuse;
            public TextureData Normal;
            public float Size;
            public float Speed;
        }
        public bool FogEnabled;
        public float MinimumFogDensity;
        public float MaximumFogDensity;
        public bool ShadowsEnabled;
        enum RampChannel
        {
            Ambient = 0,
            Sun = 1,
            Fill = 3,
            Bounce = 4,
            Clear = 5,
            Fog = 6
        }

        public int GetColumn(float phase) => Math.Min(ColorRamp.Width - 1, (int)(Mathf.Clamp01(phase) * ColorRamp.Width));
        public EnvironmentLighting Sample(float phase) => SampleColumn(GetColumn(phase));
        internal EnvironmentLighting SampleColumn(int column)
        {
            var ambient = ColorAt(RampChannel.Ambient, column);
            var fill = ColorAt(RampChannel.Fill, column);
            var bounce = ColorAt(RampChannel.Bounce, column);
            var sun = ColorAt(RampChannel.Sun, column);
            return new EnvironmentLighting(ambient + fill * Intensity(fill), ambient, ambient + bounce * Intensity(bounce), sun, Intensity(sun), ColorAt(RampChannel.Clear, column), ColorAt(RampChannel.Fog, column));
        }

        Color ColorAt(RampChannel channel, int column)
        {
            int offset = ((int)channel * ColorRamp.Width + column) * 4;
            var pixels = ColorRamp.Pixels;
            return new Color32(pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);
        }

        // Native ramp alpha encodes an intensity around neutral 128, with a fourfold range.
        static float Intensity(Color color) => Math.Max(0, 4 * (color.a * 255 - 128) / 128);
    }
}
