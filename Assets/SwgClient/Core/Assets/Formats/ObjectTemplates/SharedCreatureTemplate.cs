using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.ObjectTemplates
{
    /// <summary>SCOT movement parameters. The first speed/acceleration entry is running; the second is walking.</summary>
    public readonly struct SharedCreatureTemplate
    {
        public readonly float? RunSpeed;
        public readonly float? RunAcceleration;
        public readonly string BaseTemplate;

        SharedCreatureTemplate(float? runSpeed, float? runAcceleration, string baseTemplate)
        {
            RunSpeed = runSpeed;
            RunAcceleration = runAcceleration;
            BaseTemplate = baseTemplate;
        }

        public static SharedCreatureTemplate Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var creature = IffNode.Form(roots, "SCOT");
            float? speed = null;
            float? acceleration = null;
            var speedField = SharedObjectTemplate.Field(creature, "speed");
            if (speedField != null && speedField.U32() != 0 && speedField.Byte() == 1 && speedField.Byte() == 0x20)
                speed = speedField.Float();
            var accelerationField = SharedObjectTemplate.Field(creature, "acceleration");
            if (accelerationField != null && accelerationField.U32() != 0 && accelerationField.Byte() == 1 && accelerationField.Byte() == 0x20)
                acceleration = accelerationField.Float();
            var inherited = roots.FirstOrDefault()?.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedCreatureTemplate(speed, acceleration, path);
        }
    }
}
