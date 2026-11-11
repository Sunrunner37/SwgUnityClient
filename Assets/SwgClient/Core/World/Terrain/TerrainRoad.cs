using System;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Trn;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Native AROA centerline, baked HDTA height samples, and separate height/shader shoulders.</summary>
    internal sealed class TerrainRoad
    {
        readonly Vector2[] points;
        readonly Vector3[][] segments;
        readonly float[] fixedHeights;
        readonly float halfWidth;
        readonly int shader;
        readonly int featherType;
        readonly float heightFeather;
        readonly float shaderFeather;
        readonly bool fixedHeight;
        readonly float minX;
        readonly float maxX;
        readonly float minZ;
        readonly float maxZ;
        public TerrainRoad(TrnRoad definition)
        {
            halfWidth = definition.Width * .5f;
            points = definition.Points;
            segments = definition.Segments;
            fixedHeights = definition.FixedHeights;
            shader = definition.ShaderFamily;
            featherType = definition.HeightFeatherType;
            heightFeather = definition.HeightFeather;
            shaderFeather = definition.ShaderFeather;
            fixedHeight = definition.FixedHeight;
            minX = points.Min(point => point.x) - halfWidth;
            maxX = points.Max(point => point.x) + halfWidth;
            minZ = points.Min(point => point.y) - halfWidth;
            maxZ = points.Max(point => point.y) + halfWidth;
        }

        public void Apply(float x, float z, float weight, ref TerrainSample sample)
        {
            if (weight <= 0 || x < minX || x > maxX || z < minZ || z > maxZ)
                return;
            float best = halfWidth * halfWidth;
            int segment = -1;
            float fraction = 0;
            var position = new Vector2(x, z);
            for (int i = 0; i < points.Length - 1; i++)
            {
                var direction = points[i + 1] - points[i];
                float length = direction.sqrMagnitude;
                if (length == 0)
                    continue;
                float t = Math.Max(0, Math.Min(1, Vector2.Dot(position - points[i], direction) / length));
                float distance = (position - (points[i] + direction * t)).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    segment = i;
                    fraction = t;
                }
            }

            if (segment < 0)
                return;
            float distanceToCenter = (float)Math.Sqrt(best);
            var projected = points[segment] + (points[segment + 1] - points[segment]) * fraction;
            float desired = sample.Height;
            if (fixedHeight && fixedHeights.Length == points.Length)
                desired = fixedHeights[segment] + (fixedHeights[segment + 1] - fixedHeights[segment]) * fraction;
            else if (segment < segments.Length && segments[segment].Length > 0)
                desired = Interpolate(segments[segment], projected);
            if (distanceToCenter <= halfWidth * (1 - heightFeather))
            {
                sample.Height = desired;
                sample.ClearFlora();
            }
            else
            {
                float blend = fixedHeight ? TerrainFeather.Apply(1 - (distanceToCenter - halfWidth * (1 - heightFeather)) / (halfWidth * heightFeather), featherType) : 1 - distanceToCenter / halfWidth;
                sample.Height += (desired - sample.Height) * blend;
            }

            if (distanceToCenter <= halfWidth * (1 - shaderFeather))
                sample.ShaderFamily = shader;
        }

        static float Interpolate(Vector3[] samples, Vector2 point)
        {
            bool useX = Math.Abs(samples[samples.Length - 1].x - samples[0].x) >= Math.Abs(samples[samples.Length - 1].z - samples[0].z);
            float value = useX ? point.x : point.y;
            for (int i = 0; i < samples.Length - 1; i++)
            {
                float first = useX ? samples[i].x : samples[i].z;
                float second = useX ? samples[i + 1].x : samples[i + 1].z;
                if (value >= Math.Min(first, second) && value <= Math.Max(first, second))
                {
                    float t = second == first ? 0 : (value - first) / (second - first);
                    return samples[i].y + (samples[i + 1].y - samples[i].y) * t;
                }
            }

            return samples[(point - new Vector2(samples[0].x, samples[0].z)).sqrMagnitude < (point - new Vector2(samples[samples.Length - 1].x, samples[samples.Length - 1].z)).sqrMagnitude ? 0 : samples.Length - 1].y;
        }
    }
}
