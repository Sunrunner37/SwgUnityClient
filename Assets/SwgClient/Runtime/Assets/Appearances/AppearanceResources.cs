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
        public AppearanceResources(AppearanceData appearance, SwgMaterialFactory factory, Shader worldShader, Shader characterShader, Shader particleShader)
        {
            name = appearance.Key;
            parts = Enumerable.Range(0, appearance.Parts.Count).Select(i => MeshPartResources.FromAppearance(appearance, i, factory, appearance.Parts[i].UseCharacterShader ? characterShader : worldShader)).ToArray();
            particles = appearance.Particles.Select(effect => new ParticleEffectResources(effect, factory, worldShader, characterShader, particleShader)).ToArray();
        }

        public GameObject Create(Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            for (int i = 0; i < parts.Length; i++)
            {
                var part = new GameObject("Mesh " + i);
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = parts[i].Mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = parts[i].Material;
            }

            foreach (var effect in particles)
                effect.Create(root.transform);
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
