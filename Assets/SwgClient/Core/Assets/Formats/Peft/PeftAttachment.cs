using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Peft
{
    public sealed class PeftAttachment
    {
        public string Path;
        public float StartMinimum;
        public float StartMaximum;
        public bool KillWhenParticleDies;
        public int Spawn;
        internal static PeftAttachment Read(IffNode form)
        {
            var version = PeftFile.Version(form, 0, 1);
            var reader = new ByteReader(version.Chunk("0000"));
            var result = new PeftAttachment
            {
                Path = reader.CString(),
                StartMinimum = reader.Float(),
                StartMaximum = reader.Float()
            };
            if (version.Kind == "0000")
                reader.Skip(20); // Retired timing/LOD parameters.
            result.KillWhenParticleDies = reader.Byte() != 0;
            result.Spawn = version.Kind == "0000" ? 2 : (int)reader.U32();
            return result;
        }
    }
}
