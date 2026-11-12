using System;
using System.Text.RegularExpressions;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.Networking.Chat
{
    internal sealed class ChatText
    {
        readonly StringTableLocalizer localizer;
        readonly ProseFormatter formatter;
        readonly WorldState state;
        static readonly Regex colors = new Regex(@"\\#(?:[0-9a-fA-F]{6}|\.)", RegexOptions.Compiled);

        public ChatText(StringTableLocalizer localizer, ProseFormatter formatter, WorldState state)
        {
            this.localizer = localizer;
            this.formatter = formatter;
            this.state = state;
        }

        public string Format(string message, ChatParameterBlock parameters)
        {
            string text = localizer.Localize(message ?? "");
            foreach (var parameter in parameters.Parameters ?? Array.Empty<ChatParameterData>())
            {
                if (parameter.Type != 1)
                    continue;
                var prose = parameter.StringId;
                string template = localizer.Lookup(prose.StringId.File, prose.StringId.Key);
                string line = formatter.Format(template, ResolveParameters(prose));
                text = text.Length == 0 ? line : text + " " + line;
            }
            return colors.Replace(text, "");
        }

        internal ProseParameters ResolveParameters(LocalizedChatString prose)
            => new ProseParameters(Token(prose.User), Token(prose.Target), Token(prose.Object), prose.IntegerParameter, prose.FloatParameter);

        public string Name(ulong swgId)
        {
            if (state.SceneObjects.TryGet(swgId, out var sceneObject) && !string.IsNullOrEmpty(sceneObject.Name))
                return localizer.Localize(sceneObject.Name);
            return "Unknown";
        }

        string Token(ChatToken token)
        {
            if (!string.IsNullOrEmpty(token.Text))
                return localizer.Localize(token.Text);
            if (!string.IsNullOrEmpty(token.StringId.File))
                return localizer.Lookup(token.StringId.File, token.StringId.Key);
            return token.SwgId == 0 ? "" : Name(token.SwgId);
        }
    }
}
