using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>STOT shared tangible template metadata. An unset BoolParam inherits from the DERV template.</summary>
    public readonly struct SharedTangibleTemplate
    {
        public readonly int? ContainerCapacity;
        public readonly bool? Targetable;
        public readonly string BaseTemplate;

        SharedTangibleTemplate(bool? targetable, string baseTemplate, int? capacity)
        {
            ContainerCapacity = capacity;
            Targetable = targetable;
            BaseTemplate = baseTemplate;
        }

        public static SharedTangibleTemplate Parse(byte[] bytes)
        {
            var tangible = IffNode.Form(IffNode.Parse(bytes), "STOT");
            if (tangible == null)
                return default;
            bool? targetable = null;
            var targetField = SharedObjectTemplate.Field(tangible, "targetable");
            if (targetField != null && targetField.Byte() == 1)
                targetable = targetField.Byte() != 0;
            var inherited = tangible.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            int? capacity = null;
            var shot = IffNode.Form(tangible.Children, "SHOT");
            var capacityField = SharedObjectTemplate.Field(shot, "containerVolumeLimit");
            if (capacityField != null && capacityField.Byte() == 1 && capacityField.Byte() == 0x20)
                capacity = unchecked((int)capacityField.U32());
            return new SharedTangibleTemplate(targetable, path, capacity);
        }
    }
}
