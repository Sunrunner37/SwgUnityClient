using System;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Peft
{
    public sealed class PeftWaveform
    {
        // x=time, y=value, z=random subtraction, w=random addition.
        public Vector4[] Points;
        public int Interpolation;
        public int SampleType;
        public static PeftWaveform Read(IffNode form)
        {
            var chunk = form.Children.Single();
            int version = int.Parse(chunk.Tag);
            if (version > 2)
                throw new NotSupportedException("WVFM " + chunk.Tag);
            var reader = new ByteReader(chunk.Data);
            var result = new PeftWaveform
            {
                Interpolation = (int)reader.U32()
            };
            if (version > 0)
                result.SampleType = (int)reader.U32();
            if (version == 2)
                reader.Skip(8);
            int count = (int)reader.U32();
            result.Points = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                float time = reader.Float();
                float value = reader.Float();
                float first = reader.Float();
                float second = reader.Float();
                result.Points[i] = new Vector4(time, value, version == 2 ? first : second, version == 2 ? second : first);
            }

            return result;
        }

        public float Value(float time, float random = .5f)
        {
            int next = 0;
            while (next + 1 < Points.Length && Points[next].x < time)
                next++;
            var right = Points[next];
            var left = Points[Math.Max(0, next - 1)];
            var point = Vector4.Lerp(left, right, right.x == left.x ? 0 : (time - left.x) / (right.x - left.x));
            return point.y - point.z + (point.z + point.w) * random;
        }

        public AnimationCurve Curve(float random, float scale = 1)
        {
            var keys = Points.Select(p => new Keyframe(p.x, (p.y - p.z + (p.z + p.w) * random) * scale)).ToArray();
            // Linear native waveforms must not acquire Unity's default smooth overshoot.
            for (int i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    keys[i].inTangent = (keys[i].value - keys[i - 1].value) / Math.Max(.00001f, keys[i].time - keys[i - 1].time);
                if (i + 1 < keys.Length)
                    keys[i].outTangent = (keys[i + 1].value - keys[i].value) / Math.Max(.00001f, keys[i + 1].time - keys[i].time);
            }

            if (Interpolation == 1)
                for (int i = 1; i + 1 < keys.Length; i++)
                    keys[i].inTangent = keys[i].outTangent = (keys[i + 1].value - keys[i - 1].value) / (keys[i + 1].time - keys[i - 1].time);
            return new AnimationCurve(keys);
        }
    }
}
