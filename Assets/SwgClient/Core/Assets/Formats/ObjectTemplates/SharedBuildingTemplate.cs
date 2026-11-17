using System.Linq;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>SBOT building metadata. An unset interior layout inherits through DERV; an empty string disables it.</summary>
    public readonly struct SharedBuildingTemplate
    {
        public readonly string InteriorLayout;
        public readonly string BaseTemplate;
        SharedBuildingTemplate(string layout, string parent) { InteriorLayout = layout; BaseTemplate = parent; }
        public static SharedBuildingTemplate Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var building = IffNode.Form(roots, "SBOT");
            var field = SharedObjectTemplate.Field(building, "interiorLayoutFileName");
            string layout = field != null && field.Byte() == 1 ? field.CString() : null;
            var inherited = roots.FirstOrDefault()?.Children.FirstOrDefault(node => node.Kind == "DERV");
            string parent = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedBuildingTemplate(layout, parent);
        }
    }
}
