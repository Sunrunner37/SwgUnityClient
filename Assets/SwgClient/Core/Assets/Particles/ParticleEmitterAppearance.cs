using Swg.Client.Core.Assets.Formats.Peft;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Particles
{
    /// <summary>An emitter definition and the decoded resources it references.</summary>
    public sealed class ParticleEmitterAppearance
    {
        public PeftEmitter Definition;
        public TextureData Texture;
        public bool Additive;
        public AppearanceData Mesh;
        public AppearanceData[] Attachments;
    }
}
