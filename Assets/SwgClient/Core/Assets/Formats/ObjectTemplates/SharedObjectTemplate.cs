using System;
using System.Text;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>SHOT metadata shared by tangible and intangible templates. Unset fields inherit through DERV.</summary>
    public readonly struct SharedObjectTemplate
    {
        public readonly int? GameObjectType;
        public readonly string BaseTemplate;

        SharedObjectTemplate(int? gameObjectType, string baseTemplate)
        {
            GameObjectType = gameObjectType;
            BaseTemplate = baseTemplate;
        }

        // List parameters interleave named headers, nested forms and unnamed binary XXXX records.
        // Match the requested name before interpreting a record as a string or value.
        internal static ByteReader Field(IffNode form, string name)
        {
            if (form == null) return null;
            var prefix = Encoding.ASCII.GetBytes(name + "\0");
            foreach (var field in form.Children.Where(node => node.Tag == "FORM" && node.Kind != "DERV").SelectMany(node => node.Children).Where(node => node.Tag == "XXXX"))
                if (field.Data.AsSpan().StartsWith(prefix))
                    return new ByteReader(field.Data) { Position = prefix.Length };
            return null;
        }

        public static SharedObjectTemplate Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var shot = IffNode.Form(roots, "SHOT");
            int? gameObjectType = null;
            var field = Field(shot, "gameObjectType");
            if (field != null && field.Byte() == 1 && field.Byte() == 0x20)
                gameObjectType = unchecked((int)field.U32());
            var inherited = roots.FirstOrDefault()?.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedObjectTemplate(gameObjectType, path);
        }
    }
}