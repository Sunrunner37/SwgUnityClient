using Swg.Client.Runtime.Assets.Appearances;
using System;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Peft;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Particles;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.Assets.Particles
{
    /// <summary>Shared GPU resources for an authored effect; instances own their simulation, not textures/materials.</summary>
    public sealed class ParticleEffectResources : IDisposable
    {
        readonly ParticleAppearance source;
        sealed class EmitterResources
        {
            public PeftEmitter Definition;
            public MeshPartResources[] Parts;
            public AppearanceResources[] Attachments;
        }

        readonly EmitterResources[] emitters;
        public ParticleEffectResources(ParticleAppearance source, SwgMaterialFactory factory, Shader worldShader, Shader characterShader, Shader shader)
        {
            this.source = source;
            emitters = source.Emitters.Select(emitter =>
            {
                var parts = emitter.Mesh?.Parts;
                var uploaded = new MeshPartResources[parts?.Count ?? 1];
                for (int i = 0; i < uploaded.Length; i++)
                {
                    var part = parts == null ? null : parts[i];
                    var material = new Material(shader);
                    material.SetFloat("_DstBlend", emitter.Additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                    var texture = part == null ? emitter.Texture : part.Diffuse;
                    if (part == null && emitter.Definition.Orientation == 2 && texture != null)
                        texture = RotateVelocityFrames(texture, emitter.Definition.FramesPerColumn);
                    material.SetTexture("_BaseMap", factory.Texture(texture, TextureWrapMode.Clamp));
                    uploaded[i] = new MeshPartResources(source.Path + " particle " + i, part, material);
                }

                var attachments = new AppearanceResources[emitter.Attachments.Length];
                for (int i = 0; i < attachments.Length; i++)
                {
                    var appearance = emitter.Attachments[i];
                    attachments[i] = new AppearanceResources(appearance, factory, worldShader, characterShader, shader);
                }

                return new EmitterResources
                {
                    Definition = emitter.Definition,
                    Parts = uploaded,
                    Attachments = attachments
                };
            }).ToArray();
        }

        public GameObject Create(Transform parent)
        {
            var root = new GameObject(source.Path);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = source.Transform.GetColumn(3);
            root.transform.localRotation = source.Transform.rotation;
            root.transform.localScale = source.Transform.lossyScale * source.Effect.Scale;
            foreach (var emitter in emitters)
            {
                uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
                for (int j = 0; j < emitter.Parts.Length; j++)
                {
                    var definition = emitter.Definition;
                    var child = new GameObject(definition.Name);
                    child.transform.SetParent(root.transform, false);
                    var system = child.AddComponent<ParticleSystem>();
                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    system.useAutoRandomSeed = false;
                    system.randomSeed = seed; // Keep all shader parts of a mesh particle in the same simulation.
                    Configure(system, definition, emitter.Parts[j].Mesh, emitter.Parts[j].Material);
                    var playback = child.AddComponent<ParticleEmitterPlayback>();
                    playback.Initialize(system, definition, source.Effect.PlaybackRate);
                    if (j == 0 && emitter.Attachments.Length > 0)
                        child.AddComponent<ParticleAttachments>().Initialize(system, definition.Attachments, emitter.Attachments);
                    system.Play();
                }
            }

            return root;
        }

        static ParticleSystem.MinMaxCurve Curve(PeftWaveform wave, float scale = 1) => new ParticleSystem.MinMaxCurve(1, wave.Curve(0, scale), wave.Curve(1, scale));
        static void Configure(ParticleSystem system, PeftEmitter emitter, Mesh mesh, Material material)
        {
            var main = system.main;
            main.playOnAwake = false;
            main.duration = Mathf.Max(.01f, (emitter.LifetimeMinimum + emitter.LifetimeMaximum) * .5f + emitter.LoopDelay);
            main.loop = emitter.Loops < 0 || emitter.Loops > 1;
            main.startDelay = emitter.Delay;
            main.startLifetime = Curve(emitter.Waves[14]);
            main.startSpeed = Curve(emitter.Waves[10]);
            main.maxParticles = Mathf.Max(1, Mathf.CeilToInt(emitter.MaximumParticles));
            main.simulationSpace = emitter.LocalSpace ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.cullingMode = emitter.Attachments.Length > 0 ? ParticleSystemCullingMode.AlwaysSimulate : ParticleSystemCullingMode.Automatic;
            main.startRotation = emitter.RandomRotation ? new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2) : 0;
            main.gravityModifier = Curve(emitter.Waves[15]);
            main.startSize3D = true;
            main.startSizeX = 1;
            main.startSizeY = 1;
            main.startSizeZ = 1;
            var emission = system.emission;
            if (emitter.OneShot)
            {
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)emitter.BurstMinimum, (short)emitter.BurstMaximum) });
            }
            else if (emitter.GenerationType == 1)
                emission.rateOverDistance = Curve(emitter.Waves[9]);
            else
                emission.rateOverTime = Curve(emitter.Waves[9]);
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = emitter.Direction == 1 ? ParticleSystemShapeType.Cone : emitter.Shape switch
            {
                0 => ParticleSystemShapeType.Circle,
                1 => ParticleSystemShapeType.Sphere,
                2 => ParticleSystemShapeType.Rectangle,
                3 => ParticleSystemShapeType.Box,
                _ => ParticleSystemShapeType.SingleSidedEdge
            };
            shape.rotation = new Vector3(-90, 0, 0);
            // Native directional emitters may start particles far along their emission axis (e.g. falling waypoint beams).
            shape.position = emitter.Direction == 1 ? Vector3.up * emitter.Waves[6].Value(0) : Vector3.zero;
            shape.radius = Mathf.Max(.001f, emitter.Waves[7].Value(0));
            shape.angle = emitter.Waves[8].Value(0);
            shape.randomDirectionAmount = emitter.Direction == 0 ? 1 : 0;
            bool velocityBillboard = mesh == null && emitter.Orientation == 2;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.separateAxes = true;
            if (mesh == null)
            {
                // Native quad length/width are half-extents. Unity Stretch uses X along velocity.
                var length = Curve(emitter.ParticleWaves[1], 2);
                var width = emitter.LinkedDimensions ? length : Curve(emitter.ParticleWaves[2], 2);
                size.x = velocityBillboard ? length : width;
                size.y = velocityBillboard ? width : length;
            }
            else
                size.x = size.y = Curve(emitter.ParticleWaves[0]);
            size.z = size.y;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = Curve(emitter.SpeedScale);
            if (mesh == null && emitter.FrameCount > 1)
            {
                var animation = system.textureSheetAnimation;
                animation.enabled = true;
                animation.numTilesX = Math.Max(1, emitter.FramesPerColumn);
                animation.numTilesY = Math.Max(1, emitter.FramesPerColumn);
                animation.timeMode = emitter.FramesPerSecond < 0 ? ParticleSystemAnimationTimeMode.Lifetime : ParticleSystemAnimationTimeMode.FPS;
                animation.fps = Mathf.Max(0, emitter.FramesPerSecond);
                animation.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, (emitter.LastFrame - emitter.FirstFrame + 1f) / emitter.FrameCount));
                animation.startFrame = emitter.FirstFrame / (float)emitter.FrameCount;
            }

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = emitter.Visible && emitter.TextureVisible;
            renderer.renderMode = mesh == null ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Mesh;
            renderer.alignment = emitter.Orientation == 0 ? ParticleSystemRenderSpace.View : ParticleSystemRenderSpace.Velocity;
            if (velocityBillboard)
            {
                // Native bank-to-camera quads keep their long axis along velocity while facing the camera.
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.freeformStretching = true;
                renderer.rotateWithStretchDirection = true;
                renderer.velocityScale = 0;
                renderer.cameraVelocityScale = 0;
                renderer.lengthScale = 1;
            }
            if (mesh != null)
                renderer.mesh = mesh;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 1;
        }

        // Unity Stretch maps U along velocity; native quads map V along it.
        // Rotate each animation cell independently so atlas frame ordering remains unchanged.
        internal static TextureData RotateVelocityFrames(TextureData source, int tiles)
        {
            tiles = Math.Max(1, tiles);
            int width = source.Width / tiles;
            int height = source.Height / tiles;
            var rotated = new TextureData(source.Height, source.Width);
            for (int row = 0; row < tiles; row++)
                for (int column = 0; column < tiles; column++)
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                        {
                            int from = ((row * height + y) * source.Width + column * width + x) * 4;
                            int to = ((row * width + width - 1 - x) * rotated.Width + column * height + y) * 4;
                            Buffer.BlockCopy(source.Pixels, from, rotated.Pixels, to, 4);
                        }
            return rotated;
        }

        public void Dispose()
        {
            foreach (var emitter in emitters)
            {
                foreach (var attachment in emitter.Attachments)
                    attachment.Dispose();
                foreach (var part in emitter.Parts)
                    part.Dispose();
            }
        }
    }
}
