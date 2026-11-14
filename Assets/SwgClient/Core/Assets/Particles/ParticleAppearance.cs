using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Peft;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Appearances;
using UnityEngine;

namespace Swg.Client.Core.Assets.Particles
{
    /// <summary>Decoded effect and its TRE resources; no Unity objects are created on the asset worker.</summary>
    public sealed class ParticleAppearance
    {
        public readonly string Path;
        public readonly bool IsVehicleExhaust;
        public readonly List<ParticleEmitterAppearance> Emitters = new List<ParticleEmitterAppearance>();
        public readonly Matrix4x4 Transform;
        public readonly PeftFile Effect;
        ParticleAppearance(ParticleAppearance source, Matrix4x4 transform, bool vehicleExhaust)
        {
            Path = source.Path;
            IsVehicleExhaust = source.IsVehicleExhaust || vehicleExhaust;
            Transform = transform * source.Transform;
            Effect = source.Effect;
            Emitters.AddRange(source.Emitters);
        }
        public ParticleAppearance WithTransform(Matrix4x4 transform, bool vehicleExhaust = false) => new ParticleAppearance(this, transform, vehicleExhaust);

        public ParticleAppearance(TreAssetLoader assets, string path, Matrix4x4 transform, IffNode root, Func<string, AppearanceData> loadChild)
        {
            Path = path;
            Transform = transform;
            Effect = PeftFile.Parse(root);
            foreach (var emitter in Effect.Emitters)
            {
                TextureData texture = null;
                AppearanceData mesh = null;
                if (!string.IsNullOrEmpty(emitter.MeshPath))
                    mesh = loadChild(emitter.MeshPath);
                else if (!string.IsNullOrEmpty(emitter.ShaderPath) && ShtFile.TryGetMainTexture(assets.Read(emitter.ShaderPath), out string diffuse))
                    texture = TextureData.Load(assets, diffuse);
                Emitters.Add(new ParticleEmitterAppearance { Definition = emitter, Texture = texture, Additive = !string.IsNullOrEmpty(emitter.ShaderPath) && ShtFile.UsesAdditiveBlending(assets, emitter.ShaderPath), Mesh = mesh, Attachments = emitter.Attachments.Select(attachment => loadChild(attachment.Path)).ToArray() });
            }
        }
    }
}
