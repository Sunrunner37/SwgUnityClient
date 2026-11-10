using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Swg.Client.Core.Assets.Formats.Ui
{
    public sealed class UiNode
    {
        public readonly Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<UiNode> Children = new List<UiNode>();
        public string Get(string key, string fallback = "") => Attributes.TryGetValue(key, out var value) ? value : fallback;
        public UiNode Find(string name)
        {
            if (Get("Name") == name)
                return this;
            foreach (var child in Children)
            {
                var result = child.Find(name);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static UiNode Parse(string text)
        {
            text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);
            var stack = new Stack<UiNode>();
            stack.Push(new UiNode());
            int cursor = 0;
            while ((cursor = text.IndexOf('<', cursor)) >= 0)
            {
                int position = cursor + 1;
                bool closing = position < text.Length && text[position] == '/';
                if (closing)
                    position++;
                if (position >= text.Length || !Word(text[position]))
                {
                    cursor++;
                    continue;
                }

                while (position < text.Length && Word(text[position]))
                    position++;
                int end = text.IndexOf('>', position);
                if (end < 0)
                    break;
                if (closing)
                {
                    if (stack.Count < 2)
                        throw new InvalidDataException("Unbalanced UI markup");
                    var node = stack.Pop();
                    stack.Peek().Children.Add(node);
                }
                else
                {
                    var child = new UiNode();
                    ReadAttributes(text, position, end, child);
                    if (text[end - 1] == '/')
                        stack.Peek().Children.Add(child);
                    else
                    {
                        if (stack.Count > 128)
                            throw new InvalidDataException("UI nesting too deep");
                        stack.Push(child);
                    }
                }

                cursor = end + 1;
            }

            if (stack.Count != 1)
                throw new InvalidDataException("Unclosed UI markup");
            return stack.Pop();
        }

        static void ReadAttributes(string text, int position, int end, UiNode node)
        {
            while (position < end)
            {
                if (!Word(text[position]) && text[position] != '.')
                {
                    position++;
                    continue;
                }

                int nameStart = position;
                while (position < end && (Word(text[position]) || text[position] == '.'))
                    position++;
                int nameEnd = position;
                while (position < end && char.IsWhiteSpace(text[position]))
                    position++;
                if (position >= end || text[position] != '=')
                    continue;
                position++;
                while (position < end && char.IsWhiteSpace(text[position]))
                    position++;
                if (position >= end || (text[position] != '\'' && text[position] != '"'))
                    continue;
                char quote = text[position++];
                int valueStart = position;
                int valueEnd = text.IndexOf(quote, valueStart);
                if (valueEnd < 0 || valueEnd >= end)
                    continue;
                node.Attributes[text.Substring(nameStart, nameEnd - nameStart)] = text.Substring(valueStart, valueEnd - valueStart);
                position = valueEnd + 1;
            }
        }

        static bool Word(char value)
        {
            if (char.IsLetterOrDigit(value) || value == '_')
                return true;
            var category = char.GetUnicodeCategory(value);
            return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.ConnectorPunctuation;
        }
    }
}
