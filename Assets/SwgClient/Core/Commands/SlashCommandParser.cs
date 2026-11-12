namespace Swg.Client.Core.Commands
{
    public static class SlashCommandParser
    {
        public readonly struct Input
        {
            public readonly string Name;
            public readonly string Arguments;
            public bool IsCommand => Name != null;
            public Input(string name, string arguments)
            {
                Name = name;
                Arguments = arguments;
            }
        }

        // Preserve argument contents: commands interpret their own arguments.
        public static Input Parse(string text)
        {
            text = (text ?? "").Trim();
            if (!text.StartsWith("/"))
                return new Input(null, text);
            int end = 1;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
                end++;
            return new Input(text.Substring(1, end - 1), text.Substring(end).Trim());
        }
    }
}
