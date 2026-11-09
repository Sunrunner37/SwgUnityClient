using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Networking.Login;

namespace Swg.Client.Core.Characters.Appearance
{
    /// <summary>
    /// Reads the native CADF/0000 .ca cache directly from the configured installation.
    /// Character-list packets carry only template CRC/name/IDs, not this appearance state.
    /// </summary>
    public static class CharacterAppearanceCache
    {
        const int MaxCacheBytes = 1024 * 1024;
        public static CachedCharacterAppearance Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes.Length > MaxCacheBytes)
                throw new InvalidDataException("Invalid character appearance cache size");
            var roots = IffNode.Parse(bytes);
            if (roots.Count != 1 || roots[0].Kind != "CADF")
                throw new InvalidDataException("Not a CADF character cache");
            var root = roots[0];
            var versions = root.Children.Where(n => n.Tag == "FORM").ToArray();
            if (versions.Length != 1 || versions[0].Kind != "0000")
                throw new InvalidDataException("Unsupported character cache version");
            var version = versions[0];
            var data = version.Children.Where(n => n.Tag == "DATA").ToArray();
            if (data.Length != 1)
                throw new InvalidDataException("Missing or duplicated character cache DATA");
            var body = new ByteReader(data[0].Data);
            string template = Template(ReadString(body));
            var customization = PackedCustomization.Decode(ReadBytes(body));
            if (body.Remaining != 0)
                throw new InvalidDataException("Unexpected character DATA fields");
            string planet = "";
            var planets = version.Children.Where(n => n.Tag == "PLAN").ToArray();
            if (planets.Length > 1)
                throw new InvalidDataException("Duplicated character planet");
            if (planets.Length == 1)
            {
                var p = new ByteReader(planets[0].Data);
                planet = ReadString(p);
                if (p.Remaining != 0)
                    throw new InvalidDataException("Unexpected planet data");
            }

            var wearables = new List<CachedWearableAppearance>();
            var wear = version.Children.Where(n => n.Kind == "WEAR").ToArray();
            if (wear.Length > 1)
                throw new InvalidDataException("Duplicated wearable form");
            if (wear.Length == 1)
                foreach (var item in wear[0].Children.Where(n => n.Tag == "ITEM"))
                {
                    if (wearables.Count >= 256)
                        throw new InvalidDataException("Too many cached wearable items");
                    var r = new ByteReader(item.Data);
                    var entry = new CachedWearableAppearance
                    {
                        TemplatePath = Template(ReadString(r)),
                        Customization = PackedCustomization.Decode(ReadBytes(r))
                    };
                    if (r.Remaining != 4)
                        throw new InvalidDataException("Unsupported cached ITEM record");
                    entry.ArrangementHint = r.U32();
                    wearables.Add(entry);
                }

            return new CachedCharacterAppearance
            {
                TemplatePath = template,
                Customization = customization,
                Planet = planet,
                Wearables = wearables.AsReadOnly(),
                FromCache = true,
                SourcePath = "",
                Diagnostic = ""
            };
        }

        public static bool TryReadProfile(string gameDirectory, string username, string galaxyName, ulong objectId, out CachedCharacterAppearance appearance, out string diagnostic)
        {
            appearance = null;
            diagnostic = "Cached appearance is unavailable for this character.";
            try
            {
                string root = Path.GetFullPath(gameDirectory);
                string profiles = Path.Combine(root, "profiles");
                string user = Path.Combine(profiles, Segment(username));
                string galaxy = Path.Combine(user, Segment(galaxyName));
                string path = Path.GetFullPath(Path.Combine(galaxy, objectId.ToString(CultureInfo.InvariantCulture) + ".ca"));
                string confined = Path.GetFullPath(profiles).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!path.StartsWith(confined, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Profile path escapes configured installation");
                // Do not follow profile-directory or cache-file reparse points outside the selected account.
                foreach (string part in new[]
                {
                    profiles,
                    user,
                    galaxy,
                    path
                }

                )
                    if ((File.Exists(part) || Directory.Exists(part)) && (File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Linked profile paths are not supported");
                if (!File.Exists(path))
                    return false;
                if (new FileInfo(path).Length > MaxCacheBytes)
                    throw new InvalidDataException("Oversized character cache");
                appearance = Parse(File.ReadAllBytes(path));
                appearance.SourcePath = path;
                diagnostic = "";
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                diagnostic = "Cached appearance could not be read: " + e.Message;
                return false;
            }
        }

        public static CachedCharacterAppearance LoadOrFallback(TreAssetLoader loader, string gameDirectory, string username, string galaxyName, CharacterInfo character)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (TryReadProfile(gameDirectory, username, galaxyName, character.ObjectId, out var cached, out var diagnostic))
            {
                if (MatchesRace(cached.TemplatePath, character.Race))
                    return cached;
                diagnostic = "Cached template does not match this character's server template CRC.";
            }

            string template = ResolveTemplate(loader, character.Race);
            if (template == null)
                throw new InvalidDataException("No original player template matches the character's race CRC");
            return new CachedCharacterAppearance
            {
                TemplatePath = template,
                Customization = PackedCustomization.Decode(Array.Empty<byte>()),
                Planet = "",
                Wearables = Array.Empty<CachedWearableAppearance>(),
                FromCache = false,
                SourcePath = "",
                Diagnostic = diagnostic + " Showing the original template's default appearance."
            };
        }

        public static string ResolveTemplate(TreAssetLoader loader, uint crc)
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

        public static IReadOnlyDictionary<ushort, string> LoadCustomizationIds(TreAssetLoader loader)
        {
            var roots = IffNode.Parse(loader.Read("customization/customization_id_manager.iff"));
            var form = IffNode.Form(roots, "CIDM") ?? throw new InvalidDataException("Missing customization ID manager");
            var version = form.Children.SingleOrDefault(n => n.Kind == "0001") ?? throw new InvalidDataException("Unsupported customization ID map version");
            var chunk = version.Children.SingleOrDefault(n => n.Tag == "DATA") ?? throw new InvalidDataException("Missing customization ID data");
            var reader = new ByteReader(chunk.Data);
            var ids = new Dictionary<ushort, string>();
            while (reader.Remaining != 0)
            {
                if (ids.Count >= 4096)
                    throw new InvalidDataException("Oversized customization ID map");
                ushort id = reader.U16();
                string name = ReadString(reader);
                if (name.Length == 0 || ids.ContainsKey(id))
                    throw new InvalidDataException("Invalid customization ID entry");
                ids.Add(id, name);
            }

            return new ReadOnlyDictionary<ushort, string>(ids);
        }

        static bool MatchesRace(string shared, uint crc)
        {
            if (TemplateCrc(shared) == crc)
                return true;
            int slash = shared.LastIndexOf('/') + 1;
            return shared.Substring(slash).StartsWith("shared_", StringComparison.Ordinal) && TemplateCrc(shared.Remove(slash, 7)) == crc;
        }

        static string Segment(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "." || value == ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0 || value.IndexOf(':') >= 0 || value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith(" ", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid account or galaxy profile name");
            return value;
        }

        static string Template(string value)
        {
            string normalized = TreAssetLoader.Normalize(value);
            if (!normalized.StartsWith("object/", StringComparison.Ordinal) || !normalized.EndsWith(".iff", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid cached object template path");
            return normalized;
        }

        static byte[] ReadBytes(ByteReader reader)
        {
            int start = reader.Position;
            while (reader.Byte() != 0)
                if (reader.Position - start > 65536)
                    throw new InvalidDataException("Oversized cached string");
            var result = new byte[reader.Position - start - 1];
            Buffer.BlockCopy(reader.Bytes, start, result, 0, result.Length);
            return result;
        }

        static string ReadString(ByteReader reader)
        {
            var bytes = ReadBytes(reader);
            if (bytes.Length > 4096 || bytes.Any(b => b > 127))
                throw new InvalidDataException("Invalid cached path or name");
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
