using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>
    /// LAT (Logical Animation Table) maps animation names to native animation templates.
    /// Retains clip references and SSAT string-selector associations; does not evaluate blend templates.
    /// </summary>
    public sealed class LatFile
    {
        public sealed class Animation
        {
            public string[] Clips { get; }
            public IReadOnlyDictionary<(string Selector, string Value), string[]> Selections { get; }

            internal Animation(IffNode node)
            {
                Clips = ClipReferences(node);
                var selections = new Dictionary<(string, string), string[]>();
                ReadSelectors(node.Children, selections);
                Selections = selections;
            }
        }

        public IReadOnlyDictionary<string, Animation> Animations { get; }
        LatFile(Dictionary<string, Animation> animations) => Animations = animations;

        public static LatFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "LATT") ?? throw new InvalidDataException("Expected LATT logical animation table.");
            var animations = new Dictionary<string, Animation>(StringComparer.Ordinal);
            foreach (var node in root.Children.First(child => child.Tag == "FORM").Children.Where(child => child.Kind == "ANIM"))
            {
                string name = new ByteReader(node.Chunk("INFO")).CString();
                if (!animations.ContainsKey(name))
                    animations.Add(name, new Animation(node));
            }
            return new LatFile(animations);
        }

        static void ReadSelectors(IEnumerable<IffNode> nodes, Dictionary<(string Selector, string Value), string[]> selections)
        {
            foreach (var node in nodes)
            {
                if (node.Kind == "SSAT")
                {
                    var version = node.Children.First(child => child.Tag == "FORM");
                    if (version.Kind != "0000")
                        throw new NotSupportedException("Unsupported SSAT version " + version.Kind);
                    string selector = new ByteReader(version.Chunk("INFO")).CString();
                    var choices = version.Children.Single(child => child.Kind == "ANMS").Children.Where(child => child.Tag == "FORM").ToArray();
                    var values = new ByteReader(version.Chunk("VAL "));
                    int count = values.U16();
                    for (int i = 0; i < count; i++)
                    {
                        string value = values.CString();
                        int index = values.U16();
                        if (index >= choices.Length)
                            throw new InvalidDataException("Animation selector index outside ANMS entries.");
                        var key = (selector, value);
                        var clips = ClipReferences(choices[index]);
                        // A selector can recur inside blended templates. Keep all associated clips.
                        selections[key] = selections.TryGetValue(key, out var previous)
                            ? previous.Concat(clips).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() : clips;
                    }
                    if (values.Remaining != 0)
                        throw new InvalidDataException("Unexpected trailing SSAT selector values.");
                }
                ReadSelectors(node.Children, selections);
            }
        }

        static string[] ClipReferences(IffNode node) => IffNode.References(node.Data, "appearance/animation/", ".ans").ToArray();
    }
}
