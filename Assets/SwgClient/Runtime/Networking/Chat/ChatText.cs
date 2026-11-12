using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.Networking.Chat
{
    internal sealed class ChatText
    {
        readonly TreRuntimeAssets assets;
        readonly WorldState state;
        readonly Dictionary<string, IReadOnlyDictionary<string, string>> tables = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Func<LocalizedChatString, string>> proseTokens;
        static readonly Regex prosePlaceholders = new Regex(@"%[A-Z]{2}", RegexOptions.Compiled);
        static readonly Regex colors = new Regex(@"\\#(?:[0-9a-fA-F]{6}|\.)", RegexOptions.Compiled);
        public ChatText(TreRuntimeAssets assets, WorldState state)
        {
            this.assets = assets;
            this.state = state;
            // SWG prose templates substitute user (%TU), target (%TT), object (%TO), integer (%DI), and float (%DF) parameters into localized text.
            proseTokens = new Dictionary<string, Func<LocalizedChatString, string>>(StringComparer.Ordinal)
            {
                ["%TU"] = prose => Token(prose.User),
                ["%TT"] = prose => Token(prose.Target),
                ["%TO"] = prose => Token(prose.Object),
                ["%DI"] = prose => prose.IntegerParameter.ToString(CultureInfo.InvariantCulture),
                ["%DF"] = prose => prose.FloatParameter.ToString(CultureInfo.InvariantCulture)
            };
        }

        public string Format(string message, ChatParameterBlock parameters)
        {
            string text = Localize(message ?? "");
            foreach (var parameter in parameters.Parameters ?? Array.Empty<ChatParameterData>())
            {
                if (parameter.Type != 1)
                    continue;
                var prose = parameter.StringId;
                string line = FormatProse(Lookup(prose.StringId.File, prose.StringId.Key), prose);
                text = text.Length == 0 ? line : text + " " + line;
            }

            return colors.Replace(text, "");
        }

        internal string FormatProse(string template, LocalizedChatString prose)
            => prosePlaceholders.Replace(template, match => proseTokens.TryGetValue(match.Value, out var resolve) ? resolve(prose) : match.Value);

        public string Name(ulong swgId)
        {
            if (state.SceneObjects.TryGet(swgId, out var sceneObject) && !string.IsNullOrEmpty(sceneObject.Name))
                return Localize(sceneObject.Name);
            return "Unknown";
        }

        string Token(ChatToken token)
        {
            if (!string.IsNullOrEmpty(token.Text))
                return Localize(token.Text);
            if (!string.IsNullOrEmpty(token.StringId.File))
                return Lookup(token.StringId.File, token.StringId.Key);
            return token.SwgId == 0 ? "" : Name(token.SwgId);
        }

        string Localize(string text)
        {
            if (!text.StartsWith("@", StringComparison.Ordinal))
                return text;
            int separator = text.IndexOf(':');
            return separator > 1 ? Lookup(text.Substring(1, separator - 1), text.Substring(separator + 1)) : text;
        }

        string Lookup(string file, string key)
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
