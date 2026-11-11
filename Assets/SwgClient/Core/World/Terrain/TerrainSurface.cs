using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>A native SGRP shader family and its representative highest-weight shader variant.</summary>
    public sealed class TerrainSurface
    {
        public int FamilyId { get; private set; }
        public string Name { get; private set; }
        public string ShaderPath { get; private set; }
        public string TexturePath { get; private set; }
        public float FeatherClamp { get; private set; }
        public float TileSize { get; private set; }
        public TextureData Diffuse { get; private set; }

        public static TerrainSurface Load(TreAssetLoader assets, TrnShaderFamily family)
        {
            string shaderPath = family.Children.OrderByDescending(child => child.Weight).FirstOrDefault()?.Path;
            if (shaderPath != null)
            {
                shaderPath = TreAssetLoader.Normalize(shaderPath);
                if (!shaderPath.StartsWith("shader/", StringComparison.OrdinalIgnoreCase))
                    shaderPath = "shader/" + shaderPath;
                if (!shaderPath.EndsWith(".sht", StringComparison.OrdinalIgnoreCase))
                    shaderPath += ".sht";
            }

            var surface = new TerrainSurface
            {
                FamilyId = family.Id,
                Name = family.Name,
                ShaderPath = shaderPath,
                TileSize = family.TileSize,
                FeatherClamp = family.FeatherClamp
            };
            if (surface.ShaderPath == null)
                return surface;
            try
            {
                if (ShtFile.TryGetMainTexture(assets.Read(surface.ShaderPath), out var texturePath))
                {
                    surface.TexturePath = texturePath;
                    surface.Diffuse = TextureData.Load(assets, texturePath);
                }
                else
                    Debug.LogWarning("Terrain shader has no MAIN texture: " + surface.ShaderPath);
            }
            catch (Exception exception)when (exception is IOException || exception is InvalidOperationException || exception is NotSupportedException)
            {
                Debug.LogWarning("Terrain shader unavailable: " + surface.ShaderPath + " (" + exception.Message + ")");
            }

            return surface;
        }
    }
}
