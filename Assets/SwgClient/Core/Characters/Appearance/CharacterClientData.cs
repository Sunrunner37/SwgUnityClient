using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            string path = FindTemplateString(loader, template, "clientDataFile", new HashSet<string>(StringComparer.OrdinalIgnoreCase));
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

        // ARGD lists the slots occupied by a wearable, including inherited pants/skirt/dress arrangements.
        internal static IEnumerable<string> OccupiedSlots(TreAssetLoader loader, string template)
        {
            string path = FindTemplateString(loader, template, "arrangementDescriptorFilename", new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();
            var root = IffNode.Form(IffNode.Parse(loader.Read(path)), "ARGD") ?? throw new InvalidDataException("Expected ARGD arrangement");
            return root.Children.Single(node => node.Kind == "0000").Children.Where(node => node.Tag == "ARG ")
                .SelectMany(node => Encoding.ASCII.GetString(node.Data).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
        }

        static string FindTemplateString(TreAssetLoader loader, string template, string fieldName, HashSet<string> seen)
        {
            if (!seen.Add(template))
                throw new InvalidDataException("Cyclic character template inheritance: " + template);
            var nodes = IffNode.Parse(loader.Read(template));
            // These inherited string fields belong to SHOT; other template forms have unrelated XXXX layouts.
            var shot = IffNode.Form(nodes, "SHOT");
            if (shot != null)
                foreach (var field in shot.Children.SelectMany(version => version.Children).Where(node => node.Tag == "XXXX"))
                {
                    var reader = new ByteReader(field.Data);
                    if (reader.CString() != fieldName || reader.Byte() != 1)
                        continue;
                    string value = reader.CString();
                    // An explicit empty value overrides the inherited string.
                    return value.Length == 0 ? value : TreAssetLoader.Normalize(value);
                }
            var parent = IffNode.Form(nodes, "DERV");
            string inherited = parent == null ? null : IffNode.References(parent.Data, "object/", ".iff").FirstOrDefault();
            return inherited == null ? null : FindTemplateString(loader, inherited, fieldName, seen);
        }
    }
}
