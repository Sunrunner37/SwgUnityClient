using System.Collections.Generic;
using Swg.Client.Runtime.World.SceneObjects;
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
        readonly KeyValuePair<string, Matrix4x4>[] hardpoints;
        readonly RiderSeatData? riderSeat;
        readonly MeshPartResources[] parts;
        readonly ParticleEffectResources[] particles;
        public AppearanceResources(AppearanceData appearance, SwgMaterialFactory factory, Shader worldShader, Shader characterShader, Shader particleShader, TextureWrapMode wrapMode = TextureWrapMode.Repeat)
        {
            name = appearance.Key;
            hardpoints = appearance.Hardpoints.ToArray();
            riderSeat = appearance.RiderSeat;
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
                if (resource.AnimationFrames != null)
                    part.AddComponent<AnimatedTexture>().Initialize(part.GetComponent<Renderer>(), resource.AnimationFrames, resource.MinimumFrameSeconds, resource.MaximumFrameSeconds);
            }

            foreach (var hardpoint in hardpoints)
            {
                var point = new GameObject(hardpoint.Key);
                point.transform.SetParent(root.transform, false);
                point.transform.SetLocalPositionAndRotation(hardpoint.Value.GetColumn(3), hardpoint.Value.rotation);
            }

            if (riderSeat.HasValue)
            {
                var seat = new GameObject("Driver seat");
                seat.transform.SetParent(root.transform, false);
                seat.transform.localPosition = riderSeat.Value.Transform.GetColumn(3);
                seat.transform.localRotation = riderSeat.Value.Transform.rotation;
                seat.AddComponent<VehicleSeat>().riderPose = riderSeat.Value.PoseName;
            }
            var exhausts = new List<Transform>();
            foreach (var effect in particles)
            {
                var instance = effect.Create(root.transform);
                if (effect.IsVehicleExhaust) exhausts.Add(instance.transform);
            }
            if (exhausts.Count > 0)
                root.AddComponent<VehicleExhaust>().Initialize(exhausts.ToArray());
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
