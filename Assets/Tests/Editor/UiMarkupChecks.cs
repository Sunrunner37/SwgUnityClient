using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Runtime.UI;

namespace Swg.Client.Tests.Editor
{
    public sealed class UiMarkupChecks
    {
        [TestCase("<Page Name='root'><Image Name=\"child\" SourceRect='0,0,32,32'/></Page>")]
        [TestCase("<!-- <Bad/> --><Page Name='a<!--ignored-->b' Value='quoted / value' Empty=\"\" Tint.Color='1,2,3'><Text Text='line\ntext'/></Page>")]
        [TestCase("<Page Name='First' name='Last' ünicode='value'/><Page Name='second'/>")]
        public void PreservesExistingMarkupGrammar(string markup) => Equal(Reference(markup), UiNode.Parse(markup));
        [TestCase("<Page>")]
        [TestCase("</Page>")]
        public void RejectsUnbalancedMarkup(string markup) => Assert.Throws<InvalidDataException>(() => UiNode.Parse(markup));
        [Test, Category("NativeAssets")]
        public void OriginalUiTreesMatchThePreviousParser()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            foreach (string path in new[]
            {
                "ui/ui_styles.inc",
                "ui/ui_avatardata.inc",
                "ui/ui_avatar_creation.inc"
            }

            )
            {
                string markup = loader.Text(path);
                Equal(Reference(markup), UiNode.Parse(markup));
            }
        }

        [Test, Category("NativeAssets")]
        public void ScaledArtworkFiltersAlphaWithoutDarkFringesOrAtlasBleeding()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            const string path = "texture/ui_rebel_final.dds";
            var atlas = TextureData.Load(loader, path);
            int pixel = -1;
            for (int i = 0; i < atlas.Width * atlas.Height - 1; i++)
                if (i % atlas.Width != atlas.Width - 1 && atlas.Pixels[i * 4 + 3] == 255 && atlas.Pixels[(i + 1) * 4 + 3] == 0)
                {
                    pixel = i;
                    break;
                }

            Assert.That(pixel, Is.GreaterThanOrEqualTo(0), "The native atlas must contain an opaque-to-transparent edge.");
            int x = pixel % atlas.Width;
            int y = pixel / atlas.Width;
            var composer = new UiCompositor(loader, path, "ui/ui_styles.inc");
            var result = new TextureData(3, 1);
            composer.Copy(result, new[] { x, y, x + 2, y + 1 }, new[] { 0, 0, 3, 1 }, new[] { 255, 255, 255 }, 1);
            Assert.That(result.Pixels[3], Is.EqualTo(255));
            Assert.That(result.Pixels[7], Is.EqualTo(128));
            Assert.That(result.Pixels[11], Is.Zero);
            for (int c = 0; c < 3; c++)
                Assert.That(result.Pixels[4 + c], Is.EqualTo(atlas.Pixels[pixel * 4 + c]), "Transparent texels must not darken the edge.");
            var mirrored = new TextureData(3, 1);
            composer.Copy(mirrored, new[] { x + 2, y, x, y + 1 }, new[] { 0, 0, 3, 1 }, new[] { 255, 255, 255 }, 1);
            for (int i = 0; i < 3; i++)
                for (int c = 0; c < 4; c++)
                    Assert.That(mirrored.Pixels[i * 4 + c], Is.EqualTo(result.Pixels[(2 - i) * 4 + c]));
        }

        static void Equal(UiNode expected, UiNode actual)
        {
            CollectionAssert.AreEquivalent(expected.Attributes, actual.Attributes);
            Assert.That(actual.Children.Count, Is.EqualTo(expected.Children.Count));
            for (int i = 0; i < expected.Children.Count; i++)
                Equal(expected.Children[i], actual.Children[i]);
        }

        static readonly Regex comments = new Regex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex tags = new Regex(@"<(/?)(\w+)\b([^>]*?)(/?)>", RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex attributes = new Regex("([\\w.]+)\\s*=\\s*(['\"])(.*?)\\2", RegexOptions.Singleline | RegexOptions.Compiled);
        static UiNode Reference(string text)
        {
            text = comments.Replace(text, "");
            var stack = new Stack<UiNode>();
            stack.Push(new UiNode());
            foreach (Match m in tags.Matches(text))
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
                foreach (Match a in attributes.Matches(m.Groups[3].Value))
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
