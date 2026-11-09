using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Assets.Loading
{
    /// <summary>Reads the shipped DTII loading-card tables and resolves their STF text and DDS artwork.</summary>
    public static class LoadingScreenDataLoader
    {
        public static LoadingScreenData Load(TreAssetLoader loader, string planet, string tablePattern, string fallbackTable, int selection)
        {
            if (!Regex.IsMatch(planet ?? "", "^[a-z0-9_]+$"))
                throw new InvalidDataException("Invalid loading planet name.");
            string table = tablePattern.Replace("{planet}", planet);
            byte[] bytes;
            try
            {
                bytes = loader.Read(table);
            }
            catch (FileNotFoundException)
            {
                table = fallbackTable;
                bytes = loader.Read(table);
            }

            var cards = DataTableReader.Read<LoadingCard>(bytes).Where(card => !string.IsNullOrWhiteSpace(card.Description) && !string.IsNullOrWhiteSpace(card.PicturePath) && !string.IsNullOrWhiteSpace(card.Title)).ToList();
            if (cards.Count == 0)
                throw new InvalidDataException("No usable loading cards in " + table);
            int selected = (selection & int.MaxValue) % cards.Count;
            var strings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            for (int attempt = 0; attempt < cards.Count; attempt++)
            {
                var card = cards[(selected + attempt) % cards.Count];
                string picturePath = card.PicturePath;
                TextureData picture;
                // Shipped tables contain blank padding rows and several misspelled image paths.
                try
                {
                    picture = TextureData.Load(loader, picturePath);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }

                return new LoadingScreenData
                {
                    TablePath = table,
                    Planet = char.ToUpperInvariant(planet[0]) + planet.Substring(1),
                    Title = Resolve(card.Title),
                    Description = Resolve(card.Description),
                    PicturePath = picturePath,
                    Picture = picture,
                    PlanetImage = OptionalTexture("texture/loading/" + planet + "/ui_load_" + planet + ".dds"),
                    FlagImage = OptionalTexture("texture/loading/" + planet + "/ui_load_" + planet + "_flag.dds")
                };
            }

            if (table != fallbackTable)
                return Load(loader, planet, fallbackTable, fallbackTable, selection);
            throw new InvalidDataException("No loading artwork is available in " + table);
            string Resolve(string reference)
            {
                if (!reference.StartsWith("@", StringComparison.Ordinal))
                    return reference;
                int separator = reference.IndexOf(':');
                if (separator < 2)
                    throw new InvalidDataException("Invalid loading string reference.");
                string path = "string/en/" + reference.Substring(1, separator - 1) + ".stf";
                if (!strings.TryGetValue(path, out var entries))
                {
                    entries = StfStringTable.Parse(loader.Read(path));
                    strings.Add(path, entries);
                }

                string key = reference.Substring(separator + 1);
                if (!entries.TryGetValue(key, out string value))
                    throw new InvalidDataException("Missing loading text " + reference);
                return Regex.Replace(value, @"\\#[0-9a-fA-F]{6}|\\#\.", "");
            }

            TextureData OptionalTexture(string path)
            {
                try
                {
                    return TextureData.Load(loader, path);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }
        }
    }
}
