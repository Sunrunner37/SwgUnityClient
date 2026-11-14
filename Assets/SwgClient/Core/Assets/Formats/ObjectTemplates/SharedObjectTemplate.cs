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

        public static SharedObjectTemplate Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var shot = IffNode.Form(roots, "SHOT");
            int? gameObjectType = null;
            if (shot != null)
                foreach (var field in shot.Children.SelectMany(node => node.Children).Where(node => node.Tag == "XXXX"))
                {
                    var reader = new ByteReader(field.Data);
                    if (reader.CString() == "gameObjectType" && reader.Byte() == 1 && reader.Byte() == 0x20)
                        gameObjectType = unchecked((int)reader.U32());
                }
            var inherited = roots.FirstOrDefault()?.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedObjectTemplate(gameObjectType, path);
        }
    }
}