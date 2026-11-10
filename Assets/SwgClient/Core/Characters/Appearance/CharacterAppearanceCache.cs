using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Customization;
using Swg.Client.Core.Assets;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Swg.Client.Core.Assets.Formats.Cadf;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Networking.Login;

namespace Swg.Client.Core.Characters.Appearance
{
    /// <summary>
    /// Stores server-supplied appearances in the client-owned CADF/0000 cache.
    /// Character-list packets carry only template CRC/name/IDs, not this appearance state.
    /// </summary>
    public static class CharacterAppearanceCache
    {
        const int MaxCacheBytes = 1024 * 1024;
        public static CachedCharacterAppearance Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length > MaxCacheBytes)
                throw new InvalidDataException("Invalid character appearance cache size");
            var file = CadfFile.Parse(bytes);
            return new CachedCharacterAppearance
            {
                TemplatePath = Template(file.TemplatePath),
                Customization = PackedCustomization.Decode(file.Customization),
                Planet = file.Planet,
                Wearables = file.Wearables.Select(item => new CachedWearableAppearance { TemplatePath = Template(item.TemplatePath), Customization = PackedCustomization.Decode(item.Customization), ArrangementHint = item.Arrangement }).ToArray(),
                FromCache = true,
                SourcePath = "",
                Diagnostic = ""
            };
        }

        public static bool TryReadProfile(string cacheDirectory, string server, string username, string galaxyName, ObjectId objectId, out CachedCharacterAppearance appearance, out string diagnostic)
        {
            appearance = null;
            diagnostic = "Cached appearance is unavailable for this character.";
            try
            {
                string path = ProfilePath(cacheDirectory, server, username, galaxyName, objectId);
                if (!File.Exists(path))
                    return false;
                if (new FileInfo(path).Length > MaxCacheBytes)
                    throw new InvalidDataException("Oversized character cache");
                appearance = Parse(File.ReadAllBytes(path));
                appearance.SourcePath = path;
                diagnostic = "";
                return true;
            }
            catch (Exception e)when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidDataException || e is OverflowException)
            {
                diagnostic = "Cached appearance could not be read: " + e.Message;
                return false;
            }
        }

        public static CachedCharacterAppearance LoadOrFallback(TreAssetLoader loader, string cacheDirectory, string server, string username, string galaxyName, CharacterInfo character)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (TryReadProfile(cacheDirectory, server, username, galaxyName, character.ObjectId, out var cached, out var diagnostic))
            {
                if (ObjectTemplateNames.MatchesRace(cached.TemplatePath, character.Race))
                    return cached;
                diagnostic = "Cached template does not match this character's server template CRC.";
            }

            string template = ObjectTemplateNames.ResolvePlayerTemplate(loader, character.Race);
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
                Diagnostic = diagnostic + " Showing the TRE template's default appearance."
            };
        }

        public static void SaveProfile(string cacheDirectory, string server, string username, string galaxyName, ObjectId objectId, CachedCharacterAppearance appearance)
        {
            string path = ProfilePath(cacheDirectory, server, username, galaxyName, objectId);
            byte[] bytes = Serialize(appearance);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        public static byte[] Serialize(CachedCharacterAppearance appearance)
        {
            if (appearance == null)
                throw new ArgumentNullException(nameof(appearance));
            var file = new CadfFile
            {
                TemplatePath = Template(appearance.TemplatePath),
                Customization = appearance.Customization.Serialize(),
                Planet = appearance.Planet ?? "",
                Wearables = appearance.Wearables.Select(item => new CadfWearable { TemplatePath = Template(item.TemplatePath), Customization = item.Customization.Serialize(), Arrangement = item.ArrangementHint }).ToArray()
            };
            byte[] result = file.Serialize();
            if (result.Length > MaxCacheBytes)
                throw new InvalidDataException("Oversized character cache");
            return result;
        }

        static string ProfilePath(string cacheDirectory, string server, string username, string galaxyName, ObjectId objectId)
        {
            string root = Path.GetFullPath(cacheDirectory);
            using var hash = SHA256.Create();
            string serverKey = string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(server.ToLowerInvariant())).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            string serverDirectory = Path.Combine(root, serverKey);
            string user = Path.Combine(serverDirectory, Segment(username));
            string galaxy = Path.Combine(user, Segment(galaxyName));
            string path = Path.Combine(galaxy, objectId.ToString(CultureInfo.InvariantCulture) + ".ca");
            return path;
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
    }
}
