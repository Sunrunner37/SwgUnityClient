using UnityEngine;
using System;
using System.Linq;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class WorldTerrainPatch
    {
        public float Spacing { get; private set; }
        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; private set; }
        /// <summary>Four bytes per vertex, RGBA, from native shader family ground colors.</summary>
        public byte[] Colors { get; }
        /// <summary>Native SGRP shader family ID for each vertex; transitions interpolate across triangles.</summary>
        public int[] ShaderFamilies { get; }
        /// <summary>Only surfaces used by this patch, including their decoded native ground DDS textures.</summary>
        public TerrainSurface[] Surfaces { get; internal set; }

        internal WorldTerrainPatch(int vertices, int indices)
        {
            Vertices = new Vector3[vertices];
            Normals = new Vector3[vertices];
            Uvs = new Vector2[vertices];
            Triangles = new int[indices];
            Colors = new byte[vertices * 4];
            ShaderFamilies = new int[vertices];
            Surfaces = new TerrainSurface[0];
        }

        public static WorldTerrainPatch Build(WorldTerrain terrain, Vector3 center, float radius, float spacing, Vector4 neighborSpacing = default)
        {
            if (radius <= 0 || spacing <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius));
            int cells = checked((int)Math.Ceiling(radius * 2 / spacing));
            if (cells > 512)
                throw new ArgumentOutOfRangeException(nameof(spacing), "Terrain patches are limited to 512 cells per edge");
            float startX = (float)Math.Floor((center.x - radius) / spacing) * spacing;
            float startZ = (float)Math.Floor((center.z - radius) / spacing) * spacing;
            int edge = cells + 1;
            var patch = new WorldTerrainPatch(edge * edge, cells * cells * 6)
            {
                Spacing = spacing
            };
            for (int z = 0; z < edge; z++)
                for (int x = 0; x < edge; x++)
                {
                    float worldX = startX + x * spacing;
                    float worldZ = startZ + z * spacing;
                    var sample = terrain.Sample(worldX, worldZ);
                    int index = z * edge + x;
                    patch.Vertices[index] = new Vector3(worldX, sample.Height, worldZ);
                    patch.Uvs[index] = new Vector2(worldX / 8, worldZ / 8);
                    patch.ShaderFamilies[index] = sample.ShaderFamily;
                    patch.Colors[index * 4] = sample.Color.r;
                    patch.Colors[index * 4 + 1] = sample.Color.g;
                    patch.Colors[index * 4 + 2] = sample.Color.b;
                    patch.Colors[index * 4 + 3] = 255;
                }

            // Fine edge vertices follow the coarser neighbor's straight segments.
            void Stitch(int index, float x, float z, float neighbor, bool alongX)
            {
                if (neighbor <= spacing)
                    return;
                float coordinate = alongX ? x : z;
                float lower = (float)Math.Floor(coordinate / neighbor) * neighbor;
                float amount = (coordinate - lower) / neighbor;
                float first = terrain.SampleHeight(alongX ? lower : x, alongX ? z : lower);
                float second = terrain.SampleHeight(alongX ? lower + neighbor : x, alongX ? z : lower + neighbor);
                patch.Vertices[index].y = first + (second - first) * amount;
            }

            for (int i = 0; i < edge; i++)
            {
                Stitch(i * edge, startX, startZ + i * spacing, neighborSpacing.x, false);
                Stitch(i * edge + cells, startX + cells * spacing, startZ + i * spacing, neighborSpacing.y, false);
                Stitch(i, startX + i * spacing, startZ, neighborSpacing.z, true);
                Stitch(cells * edge + i, startX + i * spacing, startZ + cells * spacing, neighborSpacing.w, true);
            }

            for (int z = 0; z < edge; z++)
                for (int x = 0; x < edge; x++)
                {
                    float serverX = startX + x * spacing;
                    float serverZ = startZ + z * spacing;
                    float left = x > 0 ? patch.Vertices[z * edge + x - 1].y : terrain.SampleHeight(serverX - spacing, serverZ);
                    float right = x < cells ? patch.Vertices[z * edge + x + 1].y : terrain.SampleHeight(serverX + spacing, serverZ);
                    float back = z > 0 ? patch.Vertices[(z - 1) * edge + x].y : terrain.SampleHeight(serverX, serverZ - spacing);
                    float front = z < cells ? patch.Vertices[(z + 1) * edge + x].y : terrain.SampleHeight(serverX, serverZ + spacing);
                    float dx = left - right;
                    float dz = back - front;
                    float ny = spacing * 2;
                    float inverse = 1 / (float)Math.Sqrt(dx * dx + ny * ny + dz * dz);
                    patch.Normals[z * edge + x] = new Vector3(dx * inverse, ny * inverse, dz * inverse);
                }

            Vector3 Normal(float x, float z, float step)
            {
                return new Vector3(terrain.SampleHeight(x - step, z) - terrain.SampleHeight(x + step, z), step * 2, terrain.SampleHeight(x, z - step) - terrain.SampleHeight(x, z + step)).normalized;
            }

            void StitchNormal(int index, float x, float z, float neighbor, bool alongX)
            {
                if (neighbor <= spacing)
                    return;
                float coordinate = alongX ? x : z;
                float lower = (float)Math.Floor(coordinate / neighbor) * neighbor;
                var first = Normal(alongX ? lower : x, alongX ? z : lower, neighbor);
                var second = Normal(alongX ? lower + neighbor : x, alongX ? z : lower + neighbor, neighbor);
                patch.Normals[index] = Vector3.Lerp(first, second, (coordinate - lower) / neighbor).normalized;
            }

            for (int i = 0; i < edge; i++)
            {
                StitchNormal(i * edge, startX, startZ + i * spacing, neighborSpacing.x, false);
                StitchNormal(i * edge + cells, startX + cells * spacing, startZ + i * spacing, neighborSpacing.y, false);
                StitchNormal(i, startX + i * spacing, startZ, neighborSpacing.z, true);
                StitchNormal(cells * edge + i, startX + i * spacing, startZ + cells * spacing, neighborSpacing.w, true);
            }

            int triangle = 0;
            for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    if (terrain.Sample(startX + (x + .5f) * spacing, startZ + (z + .5f) * spacing).Excluded)
                        continue;
                    int a = z * edge + x;
                    patch.Triangles[triangle++] = a;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + edge + 1;
                }

            if (triangle != patch.Triangles.Length)
                patch.Triangles = patch.Triangles.Take(triangle).ToArray();
            patch.Surfaces = patch.ShaderFamilies.Distinct().Select(terrain.GetSurface).Where(surface => surface != null).ToArray();
            return patch;
        }
    }
}
