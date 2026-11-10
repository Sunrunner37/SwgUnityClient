using System;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Assets
{
    public static class ObjectTemplateNames
    {
        public static string ResolvePlayerTemplate(TreAssetLoader loader, uint crc)
        {
            // The server hashes the unshared raceFile; TRE contains the shared client template.
            return loader.Catalog().Keys.Where(p => p.StartsWith("object/creature/player/shared_", StringComparison.Ordinal) && p.EndsWith(".iff", StringComparison.Ordinal)).OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault(p => MatchesRace(p, crc));
        }

        public static uint TemplateCrc(string path)
        {
            uint crc = uint.MaxValue;
            foreach (byte b in Encoding.ASCII.GetBytes(path))
            {
                crc ^= (uint)b << 24;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04c11db7u : crc << 1;
            }

            return ~crc;
        }

        public static bool MatchesRace(string shared, uint crc)
        {
            if (TemplateCrc(shared) == crc)
                return true;
            int slash = shared.LastIndexOf('/') + 1;
            return shared.Substring(slash).StartsWith("shared_", StringComparison.Ordinal) && TemplateCrc(shared.Remove(slash, 7)) == crc;
        }
    }
}
