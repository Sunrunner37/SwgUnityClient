using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Characters.Models
{
    internal static class MeshBlendTargets
    {
        internal static void Apply(IffNode version, byte[] positions, byte[] normals, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner)
        {
            var targets = version.Children.FirstOrDefault(n => n.Kind == "BLTS");
            if (targets == null)
                return;
            foreach (var target in targets.Children.Where(n => n.Kind == "BLT "))
            {
                var info = new ByteReader(target.Chunk("INFO"));
                int positionCount = checked((int)info.U32());
                int normalCount = checked((int)info.U32());
                string name = info.CString();
                if (info.Remaining != 0)
                    throw new InvalidDataException("Unsupported blend-target INFO");
                short value;
                if (!owner.TryGetValue("/shared_owner/" + name, out value) && !own.TryGetValue("/private/" + name, out value) && !own.TryGetValue(name, out value))
                    value = 0;
                // Cached ranged-integer blend values use the native byte scale;
                // signed values (such as blend_flat_chest) must not be clamped to zero.
                float weight = value / 255f;
                ApplyDeltas(target.Children.FirstOrDefault(n => n.Tag == "POSN")?.Data, positionCount, positions, weight);
                ApplyDeltas(target.Children.FirstOrDefault(n => n.Tag == "NORM")?.Data, normalCount, normals, weight);
            }
        }

        static void ApplyDeltas(byte[] data, int count, byte[] target, float weight)
        {
            if (count == 0 && data == null)
                return;
            if (count > 200000 || data == null || data.Length != count * 16)
                throw new InvalidDataException("Invalid sparse blend-target data");
            var reader = new ByteReader(data);
            for (int i = 0; i < count; i++)
            {
                int index = checked((int)reader.U32());
                if (index < 0 || index >= target.Length / 12)
                    throw new InvalidDataException("Blend-target source index out of bounds");
                for (int axis = 0; axis < 3; axis++)
                {
                    float delta = reader.Float();
                    int offset = index * 12 + axis * 4;
                    float original = BitConverter.ToSingle(target, offset);
                    float value = original + delta * weight;
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new InvalidDataException("Invalid blended coordinate");
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, target, offset, 4);
                }
            }
        }
    }
}
