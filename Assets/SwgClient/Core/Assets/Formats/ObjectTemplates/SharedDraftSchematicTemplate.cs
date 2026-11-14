using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>SDSC draft templates identify the tangible template shown as their crafted result.</summary>
    public static class SharedDraftSchematicTemplate
    {
        public readonly struct Attribute
        {
            public readonly string Name;
            public readonly string Experiment;
            public Attribute(string name, string experiment) { Name = name; Experiment = experiment; }
        }
        public static Attribute[] Attributes(byte[] bytes)
        {
            var draft = IffNode.Form(IffNode.Parse(bytes), "SDSC");
            return draft == null ? System.Array.Empty<Attribute>() : draft.Children.SelectMany(node => node.Children)
                .Where(node => node.Kind == "DSSA")
                .Select(node => new Attribute(Label(node, "name"), Label(node, "experiment"))).ToArray();
        }
        static string Label(IffNode node, string name)
        {
            byte[] prefix = Encoding.ASCII.GetBytes(name + "\0");
            var field = node.Children.FirstOrDefault(child => child.Tag == "XXXX" && child.Data.Take(prefix.Length).SequenceEqual(prefix));
            if (field == null || field.Data.Length <= prefix.Length + 2) return "";
            var reader = new ByteReader(field.Data);
            reader.Skip(prefix.Length);
            if (reader.Byte() != 1 || reader.Byte() != 1) return "";
            string file = reader.CString();
            reader.Byte();
            string key = reader.CString();
            return key.Length == 0 ? "" : "@" + file + ":" + key;
        }
        public static string CraftedTemplate(byte[] bytes)
        {
            var draft = IffNode.Form(IffNode.Parse(bytes), "SDSC");
            if (draft == null) return null;
            foreach (var field in draft.Children.SelectMany(node => node.Children).Where(node => node.Tag == "XXXX" && node.Data.Length > 4))
            {
                byte[] name = Encoding.ASCII.GetBytes("craftedSharedTemplate\0");
                if (field.Data.Take(name.Length).SequenceEqual(name))
                    return IffNode.References(field.Data, "object/", ".iff").FirstOrDefault();
            }
            return null;
        }
    }
}
