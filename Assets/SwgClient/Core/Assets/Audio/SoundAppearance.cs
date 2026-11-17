using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using Swg.Client.Core.Assets.Formats.Sound;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.Assets.Audio
{
    public sealed class SoundAppearance
    {
        public string Path;
        public Matrix4x4 Transform;
        public SoundTemplate Settings;
        public static IEnumerable<SoundAppearance> Load(TreAssetLoader loader, string template, Matrix4x4 transform)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (template != null && seen.Add(template))
            {
                var metadata = SharedObjectTemplate.Parse(loader.Read(template));
                if (metadata.ClientDataFile != null)
                {
                    if (metadata.ClientDataFile.Length == 0) yield break;
                    var clientData = IffNode.Form(IffNode.Parse(loader.Read(metadata.ClientDataFile)), "CLDF");
                    var version = clientData.Children.Single(node => node.Kind == "0000");
                    foreach (var node in version.Children.Where(node => node.Tag == "ASND"))
                    {
                        string path = new ByteReader(node.Data).CString();
                        if (path.Length != 0) yield return new SoundAppearance { Path = path, Transform = transform, Settings = SoundTemplate.Parse(loader.Read(path)) };
                    }
                    yield break;
                }
                template = metadata.BaseTemplate;
            }
        }
    }
}
