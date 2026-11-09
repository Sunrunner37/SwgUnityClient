using UnityEngine;

namespace Swg.Client.Runtime.Assets.Shaders
{
    // An animatable opacity value that preserves textures loaded through a renderer property block.
    [ExecuteAlways]
    public sealed class RendererOpacity : MonoBehaviour
    {
        public Renderer target;
        [Range(0, 1)]
        public float opacity = 1;
        MaterialPropertyBlock properties;
        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void OnDidApplyAnimationProperties() => Apply();
        void Apply()
        {
            if (target == null)
                return;
            if (properties == null)
                properties = new MaterialPropertyBlock();
            target.GetPropertyBlock(properties);
            properties.SetFloat("_Opacity", opacity);
            target.SetPropertyBlock(properties);
        }
    }
}
