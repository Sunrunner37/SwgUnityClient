using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Shaders
{
    public sealed class SwgMaterialFactory
    {
        readonly TreRuntimeAssets assets;

        public SwgMaterialFactory(TreRuntimeAssets assets) => this.assets = assets;

        public Material Create(Shader shader, TextureData diffuse, byte[] tint,
            float cutoff, bool vertexColors, TextureWrapMode wrapMode)
        {
            var material = new Material(shader);
            material.SetTexture("_BaseMap", diffuse == null ? Texture2D.whiteTexture : assets.Texture(diffuse, wrapMode));
            material.SetColor("_Tint", tint == null || tint.Length < 3 ? Color.white : new Color32(tint[0], tint[1], tint[2], 255));
            material.SetFloat("_Cutoff", cutoff);
            if (material.HasProperty("_UseVertexColor"))
                material.SetFloat("_UseVertexColor", vertexColors ? 1 : 0);
            return material;
        }
    }
}
