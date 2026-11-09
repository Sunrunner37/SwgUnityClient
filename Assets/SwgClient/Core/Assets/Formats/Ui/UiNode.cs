using System;
using System.Collections.Generic;
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
            foreach (var c in Children)
            {
                var result = c.Find(name);
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
            foreach (Match m in Regex.Matches(text, @"<(/?)(\w+)\b([^>]*?)(/?)>", RegexOptions.Singleline))
            {
                if (m.Groups[1].Value == "/")
                {
                    if (stack.Count < 2)
                        throw new InvalidDataException("Unbalanced UI markup");
                    var node = stack.Pop();
                    stack.Peek().Children.Add(node);
                    continue;
                }

                var child = new UiNode();
                foreach (Match a in Regex.Matches(m.Groups[3].Value, "([\\w.]+)\\s*=\\s*(['\"])(.*?)\\2", RegexOptions.Singleline))
                    child.Attributes[a.Groups[1].Value] = a.Groups[3].Value;
                if (m.Groups[4].Value == "/")
                    stack.Peek().Children.Add(child);
                else
                {
                    if (stack.Count > 128)
                        throw new InvalidDataException("UI nesting too deep");
                    stack.Push(child);
                }
            }

            if (stack.Count != 1)
                throw new InvalidDataException("Unclosed UI markup");
            return stack.Pop();
        }
    }
}
