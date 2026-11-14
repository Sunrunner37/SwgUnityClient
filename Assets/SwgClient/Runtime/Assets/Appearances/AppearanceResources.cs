using System;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Particles;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Owns GPU resources for a decoded appearance, including nested particle attachments.</summary>
    public sealed class AppearanceResources : IDisposable
    {
        readonly string name;
        readonly MeshPartResources[] parts;
        readonly ParticleEffectResources[] particles;
        public AppearanceResources(AppearanceData appearance, SwgMaterialFactory factory, Shader worldShader, Shader characterShader, Shader particleShader, TextureWrapMode wrapMode = TextureWrapMode.Repeat)
        {
            name = appearance.Key;
            parts = Enumerable.Range(0, appearance.Parts.Count).Select(i => MeshPartResources.FromAppearance(appearance, i, factory, appearance.Parts[i].UseCharacterShader ? characterShader : worldShader, wrapMode)).ToArray();
            particles = appearance.Particles.Select(effect => new ParticleEffectResources(effect, factory, worldShader, characterShader, particleShader)).ToArray();
        }

        public GameObject Create(Transform parent, bool detectMovement = true, bool previewLighting = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            CharacterAnimation animation = parts.Any(part => part.Skinning != null) ? root.AddComponent<CharacterAnimation>() : null;
            if (animation != null)
                animation.detectMovement = detectMovement;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = new GameObject("Mesh " + i);
                part.transform.SetParent(root.transform, false);
                var resource = parts[i];
                if (resource.Skinning == null)
                {
                    part.AddComponent<MeshFilter>().sharedMesh = resource.Mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial = resource.Material;
                }
                else
                {
                    var renderer = part.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = resource.Mesh;
                    renderer.sharedMaterial = resource.Material;
                    renderer.bones = animation.GetBones(resource.Skinning.Skeleton);
                    renderer.rootBone = renderer.bones[0];
                    // Include arm swings and hip bob beyond the static bind-pose box.
                    renderer.localBounds = new Bounds(resource.Bounds.center, resource.Bounds.size + Vector3.one);
                }
            }

            foreach (var effect in particles)
                effect.Create(root.transform);
            if (previewLighting)
            {
                var properties = new MaterialPropertyBlock();
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.GetPropertyBlock(properties);
                    properties.SetFloat("_SwgPreview", 1);
                    renderer.SetPropertyBlock(properties);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            return root;
        }

        public void Dispose()
        {
            foreach (var effect in particles)
                effect.Dispose();
            foreach (var part in parts)
                part.Dispose();
        }
    }
}
