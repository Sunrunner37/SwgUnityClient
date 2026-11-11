using UnityEngine;
using System;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.World.Terrain
{
    internal sealed class TerrainBoundary
    {
        readonly string kind;
        readonly Vector2[] points;
        readonly float radius;
        readonly int featherType;
        readonly float feather;
        readonly float minX;
        readonly float minZ;
        readonly float maxX;
        readonly float maxZ;
        public TerrainBoundary(string kind, byte[] data)
        {
            this.kind = kind;
            var r = new ByteReader(data);
            if (kind == "BCIR")
            {
                points = new[]
                {
                    new Vector2(r.Float(), r.Float())
                };
                radius = r.Float();
            }
            else if (kind == "BREC")
                points = new[]
                {
                    new Vector2(r.Float(), r.Float()),
                    new Vector2(r.Float(), r.Float())
                };
            else
            {
                int count = checked((int)r.U32());
                if (count < 2 || count > 65536)
                    throw new System.IO.InvalidDataException("Invalid terrain boundary vertex count");
                points = new Vector2[count];
                for (int i = 0; i < count; i++)
                    points[i] = new Vector2(r.Float(), r.Float());
            }

            featherType = checked((int)r.U32());
            feather = Math.Max(0, r.Float());
            if (kind == "BPLN")
                radius = r.Float();
            minX = maxX = points[0].x;
            minZ = maxZ = points[0].y;
            foreach (var point in points)
            {
                minX = Math.Min(minX, point.x);
                maxX = Math.Max(maxX, point.x);
                minZ = Math.Min(minZ, point.y);
                maxZ = Math.Max(maxZ, point.y);
            }
        }

        public float Sample(float x, float z)
        {
            float value;
            if (kind == "BCIR")
            {
                float distance = Square(x - points[0].x) + Square(z - points[0].y);
                if (distance > radius * radius)
                    return 0;
                float inner = radius * (1 - Math.Min(feather, 1));
                value = distance <= inner * inner ? 1 : 1 - (distance - inner * inner) / (radius * radius - inner * inner);
            }
            else if (kind == "BREC")
            {
                if (x < minX || x > maxX || z < minZ || z > maxZ)
                    return 0;
                float width = Math.Min(maxX - minX, maxZ - minZ) * Math.Min(feather, 1) * 0.5f;
                value = width <= 0 ? 1 : Math.Min(1, Math.Min(Math.Min(x - minX, maxX - x), Math.Min(z - minZ, maxZ - z)) / width);
            }
            else
            {
                if (x < minX - radius || x > maxX + radius || z < minZ - radius || z > maxZ + radius)
                    return 0;
                bool inside = false;
                float distance = float.MaxValue;
                int segmentCount = kind == "BPOL" ? points.Length : points.Length - 1;
                for (int i = 0; i < segmentCount; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Length];
                    if ((a.y > z) != (b.y > z) && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                    float dx = b.x - a.x;
                    float dz = b.y - a.y;
                    float length = dx * dx + dz * dz;
                    float t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((x - a.x) * dx + (z - a.y) * dz) / length));
                    distance = Math.Min(distance, Square(x - a.x - dx * t) + Square(z - a.y - dz * t));
                }

                if (kind == "BPOL")
                {
                    if (!inside)
                        return 0;
                    value = feather <= 0 ? 1 : Math.Min(1, (float)Math.Sqrt(distance) / feather);
                }
                else
                {
                    if (distance >= radius * radius)
                        return 0;
                    float inner = radius * (1 - Math.Min(feather, 1));
                    value = distance <= inner * inner ? 1 : 1 - ((float)Math.Sqrt(distance) - inner) / (radius - inner);
                }
            }

            return Feather(value, featherType);
        }

        static float Square(float value) => value * value;
        internal static float Feather(float value, int type)
        {
            switch (type)
            {
                case 0:
                    return value;
                case 1:
                    return value * value;
                case 2:
                    return (float)Math.Sqrt(value);
                case 3:
                    return value * value * (3 - 2 * value);
                default:
                    return 0;
            }
        }
    }
}
