using UnityEngine;
using System;
using System.Linq;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class WorldTerrainPatch
    {
        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; }
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

        public static WorldTerrainPatch Build(WorldTerrain terrain, Vector3 center, float radius, float spacing)
        {
            if (radius <= 0 || spacing <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius));
            int cells = checked((int)Math.Ceiling(radius * 2 / spacing));
            if (cells > 512)
                throw new ArgumentOutOfRangeException(nameof(spacing), "Terrain patches are limited to 512 cells per edge");
            float startX = (float)Math.Floor((center.x - radius) / spacing) * spacing;
            float startZ = (float)Math.Floor((center.z - radius) / spacing) * spacing;
            int edge = cells + 1;
            var patch = new WorldTerrainPatch(edge * edge, cells * cells * 6);
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

            for (int z = 0; z < edge; z++)
                for (int x = 0; x < edge; x++)
                {
                    float dx = patch.Vertices[z * edge + Math.Max(0, x - 1)].y - patch.Vertices[z * edge + Math.Min(cells, x + 1)].y;
                    float dz = patch.Vertices[Math.Max(0, z - 1) * edge + x].y - patch.Vertices[Math.Min(cells, z + 1) * edge + x].y;
                    float ny = spacing * 2;
                    float inverse = 1 / (float)Math.Sqrt(dx * dx + ny * ny + dz * dz);
                    patch.Normals[z * edge + x] = new Vector3(dx * inverse, ny * inverse, dz * inverse);
                }

            int triangle = 0;
            for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    int a = z * edge + x;
                    patch.Triangles[triangle++] = a;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + edge + 1;
                }

            patch.Surfaces = patch.ShaderFamilies.Distinct().Select(terrain.GetSurface).Where(surface => surface != null).ToArray();
            return patch;
        }
    }
}
