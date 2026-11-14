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
            if (creature != null)
                foreach (var field in creature.Children.SelectMany(node => node.Children).Where(node => node.Tag == "XXXX"))
                {
                    var reader = new ByteReader(field.Data);
                    string name = reader.CString();
                    if (name != "speed" && name != "acceleration") continue;
                    if (reader.U32() == 0 || reader.Byte() != 1 || reader.Byte() != 0x20) continue;
                    float value = reader.Float();
                    if (name == "speed") speed = value;
                    else acceleration = value;
                }
            var inherited = roots.FirstOrDefault()?.Children.FirstOrDefault(node => node.Kind == "DERV");
            string path = inherited == null ? null : IffNode.References(inherited.Data, "object/", ".iff").FirstOrDefault();
            return new SharedCreatureTemplate(speed, acceleration, path);
        }
    }
}
