using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.Localization
{
    // Resolves SWG @file:key references through cached, TRE-backed STF tables.
    public sealed class StringTableLocalizer
    {
        readonly TreRuntimeAssets assets;
        readonly Dictionary<string, IReadOnlyDictionary<string, string>> tables = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public StringTableLocalizer(TreRuntimeAssets assets) => this.assets = assets;

        public string Localize(string text)
        {
            if (!text.StartsWith("@", StringComparison.Ordinal))
                return text;
            int separator = text.IndexOf(':');
            return separator > 1 ? Lookup(text.Substring(1, separator - 1), text.Substring(separator + 1)) : text;
        }

        public string Lookup(string file, string key)
        {
            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(key))
                return "";
            if (!tables.TryGetValue(file, out var table))
            {
                string path = "string/en/" + file.TrimStart('@') + ".stf";
                try
                {
                    table = StfStringTable.Parse(assets.Loader.Read(path));
                }
                catch (FileNotFoundException error)
                {
                    Debug.LogWarning(error.Message);
                    table = new Dictionary<string, string>();
                }
                tables.Add(file, table);
            }

            return table.TryGetValue(key, out var value) ? value : "@" + file + ":" + key;
        }
    }
}
