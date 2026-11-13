using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
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

        /// <summary>Reads the shared template's localized objectName, following native template inheritance.</summary>
        public static string DisplayName(TreAssetLoader loader, string templatePath) => StringId(loader, templatePath, "objectName");
        public static string Description(TreAssetLoader loader, string templatePath) => StringId(loader, templatePath, "detailedDescription");
        static string StringId(TreAssetLoader loader, string templatePath, string fieldName)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!string.IsNullOrEmpty(templatePath) && visited.Add(templatePath))
            {
                var shot = IffNode.Form(IffNode.Parse(loader.Read(templatePath)), "SHOT");
                if (shot == null)
                    return "";
                foreach (var field in shot.Children.SelectMany(node => node.Children).Where(node => node.Tag == "XXXX"))
                {
                    var reader = new ByteReader(field.Data);
                    if (reader.CString() != fieldName || reader.Byte() != 1)
                        continue;
                    // StringIdParam stores a single string parameter for each of file and key.
                    if (reader.Byte() != 1)
                        continue;
                    string file = reader.CString();
                    if (reader.Byte() != 1)
                        continue;
                    string key = reader.CString();
                    return file.Length > 0 && key.Length > 0 ? "@" + file + ":" + key : "";
                }
                var inherited = shot.Children.FirstOrDefault(node => node.Kind == "DERV");
                templatePath = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            }
            return "";
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
