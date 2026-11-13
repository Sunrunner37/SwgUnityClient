using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>STOT shared tangible template metadata. An unset BoolParam inherits from the DERV template.</summary>
    public readonly struct SharedTangibleTemplate
    {
        public readonly bool? Targetable;
        public readonly string BaseTemplate;

        SharedTangibleTemplate(bool? targetable, string baseTemplate)
        {
            Targetable = targetable;
            BaseTemplate = baseTemplate;
        }

        public static SharedTangibleTemplate Parse(byte[] bytes)
        {
            var tangible = IffNode.Form(IffNode.Parse(bytes), "STOT");
            if (tangible == null)
                return default;
            bool? targetable = null;
            foreach (var field in tangible.Children.SelectMany(node => node.Children).Where(node => node.Tag == "XXXX"))
            {
                var reader = new ByteReader(field.Data);
                if (reader.CString() == "targetable" && reader.Byte() == 1)
                    targetable = reader.Byte() != 0;
            }
            var inherited = tangible.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedTangibleTemplate(targetable, path);
        }
    }
}
