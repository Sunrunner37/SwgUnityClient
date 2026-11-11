using UnityEngine;

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
    }
}
