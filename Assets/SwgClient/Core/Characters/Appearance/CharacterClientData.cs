using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Characters.Appearance
{
    /// <summary>Character-specific WEAR and CSSI directives from a template's native client-data file.</summary>
    /// <seealso href="https://github.com/SWG-Source/client-tools/blob/master/src/engine/client/library/clientGame/src/shared/objectTemplate/ClientDataFile_Wearable.cpp"/>
    internal sealed class CharacterClientData
    {
        internal sealed class Wearable
        {
            public readonly List<string> Meshes = new List<string>();
            public readonly Dictionary<string, short> Customization = new Dictionary<string, short>();
            public string ObjectTemplate;
        }
        public readonly Dictionary<string, short> Customization = new Dictionary<string, short>();
        public readonly List<Wearable> Wearables = new List<Wearable>();

        public static CharacterClientData Load(TreAssetLoader loader, string template)
        {
            if (!template.StartsWith("object/", StringComparison.OrdinalIgnoreCase))
                return new CharacterClientData();
            string path = FindClientData(loader, template, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return path == null || path.Length == 0 ? new CharacterClientData() : FromRawData(loader.Read(path));
        }

        internal static CharacterClientData FromRawData(byte[] bytes)
        {
            var result = new CharacterClientData();
            var root = IffNode.Form(IffNode.Parse(bytes), "CLDF") ?? throw new InvalidDataException("Expected CLDF client data");
            var version = root.Children.First(node => node.Kind == "0000");
            foreach (var node in version.Children)
            {
                if (node.Tag == "CSSI")
                    ReadSetting(node.Data, result.Customization);
                else if (node.Kind == "WEAR")
                {
                    var wearable = new Wearable();
                    foreach (var part in node.Children)
                    {
                        if (part.Tag == "MESH")
                            wearable.Meshes.Add(TreAssetLoader.Normalize(new ByteReader(part.Data).CString()));
                        else if (part.Tag == "OBJT")
                            wearable.ObjectTemplate = TreAssetLoader.Normalize(new ByteReader(part.Data).CString());
                        else if (part.Tag == "WCSI")
                            ReadSetting(part.Data, wearable.Customization);
                    }
                    result.Wearables.Add(wearable);
                }
            }
            return result;
        }

        static void ReadSetting(byte[] bytes, Dictionary<string, short> settings)
        {
            var reader = new ByteReader(bytes);
            string name = reader.CString();
            settings[name] = checked((short)unchecked((int)reader.U32()));
        }

        static string FindClientData(TreAssetLoader loader, string template, HashSet<string> seen)
        {
            if (!seen.Add(template))
                throw new InvalidDataException("Cyclic character template inheritance: " + template);
            var nodes = IffNode.Parse(loader.Read(template));
            string ReadFields(IEnumerable<IffNode> fields)
            {
                foreach (var node in fields)
                {
                    if (node.Tag == "XXXX")
                    {
                        var reader = new ByteReader(node.Data);
                        if (reader.CString() == "clientDataFile" && reader.Byte() == 1)
                        {
                            string clientDataPath = reader.CString();
                            // An explicit empty value means this template has no client-data file.
                            return clientDataPath.Length == 0 ? clientDataPath : TreAssetLoader.Normalize(clientDataPath);
                        }
                    }
                    string value = ReadFields(node.Children);
                    if (value != null)
                        return value;
                }
                return null;
            }
            string own = ReadFields(nodes);
            if (own != null)
                return own;
            var parent = IffNode.Form(nodes, "DERV");
            string inherited = parent == null ? null : IffNode.References(parent.Data, "object/", ".iff").FirstOrDefault();
            return inherited == null ? null : FindClientData(loader, inherited, seen);
        }
    }
}
