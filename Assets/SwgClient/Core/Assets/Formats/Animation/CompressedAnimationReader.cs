using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Characters.Animation;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    public static class CompressedAnimationReader
    {
        public static SkeletalAnimation Read(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "CKAT") ?? throw new NotSupportedException("Expected compressed keyframe animation");
            var version = root.Children.First(n => n.Tag == "FORM");
            if (version.Kind != "0001")
                throw new NotSupportedException("Animation version " + version.Kind);
            var info = version.Chunk("INFO");
            float frameRate = BitConverter.ToSingle(info, 0);
            int lastFrame = BitConverter.ToUInt16(info, 4);
            if (frameRate <= 0 || float.IsNaN(frameRate) || float.IsInfinity(frameRate))
                throw new InvalidDataException("Invalid animation frame rate");
            var rotations = new List<Quaternion[]>();
            var ar = IffNode.Form(version.Children, "AROT");
            if (ar != null)
                foreach (var channel in ar.Children.Where(n => n.Tag == "QCHN"))
                {
                    var b = channel.Data;
                    int count = BitConverter.ToUInt16(b, 0);
                    if (count == 0 || b.Length != 5 + count * 6)
                        throw new InvalidDataException("Invalid rotation track");
                    var times = new int[count];
                    var values = new Quaternion[count];
                    for (int i = 0; i < count; i++)
                    {
                        times[i] = BitConverter.ToUInt16(b, 5 + i * 6);
                        values[i] = Expand(BitConverter.ToUInt32(b, 7 + i * 6), b[2], b[3], b[4]);
                    }

                    var frames = new Quaternion[lastFrame + 1];
                    int key = 0;
                    for (int frame = 0; frame <= lastFrame; frame++)
                    {
                        while (key + 1 < count && times[key + 1] <= frame)
                            key++;
                        int next = Math.Min(key + 1, count - 1);
                        float blend = times[next] == times[key] ? 0 : Mathf.InverseLerp(times[key], times[next], frame);
                        frames[frame] = Quaternion.Slerp(values[key], values[next], blend);
                    }

                    rotations.Add(frames);
                }

            var staticRotations = new List<Quaternion>();
            var sr = version.Chunk("SROT");
            for (int i = 0; i + 7 <= sr.Length; i += 7)
                staticRotations.Add(Expand(BitConverter.ToUInt32(sr, i + 3), sr[i], sr[i + 1], sr[i + 2]));
            var translations = new List<float[]>();
            var at = IffNode.Form(version.Children, "ATRN");
            if (at != null)
                foreach (var channel in at.Children.Where(n => n.Tag == "CHNL"))
                {
                    var b = channel.Data;
                    int count = BitConverter.ToUInt16(b, 0);
                    if (count == 0 || b.Length != 2 + count * 6)
                        throw new InvalidDataException("Invalid translation track");
                    var frames = new float[lastFrame + 1];
                    int key = 0;
                    for (int frame = 0; frame <= lastFrame; frame++)
                    {
                        while (key + 1 < count && BitConverter.ToUInt16(b, 2 + (key + 1) * 6) <= frame)
                            key++;
                        int next = Math.Min(key + 1, count - 1);
                        int firstTime = BitConverter.ToUInt16(b, 2 + key * 6);
                        int nextTime = BitConverter.ToUInt16(b, 2 + next * 6);
                        float blend = nextTime == firstTime ? 0 : Mathf.InverseLerp(firstTime, nextTime, frame);
                        frames[frame] = Mathf.Lerp(BitConverter.ToSingle(b, 4 + key * 6), BitConverter.ToSingle(b, 4 + next * 6), blend);
                    }

                    translations.Add(frames);
                }

            var st = version.Chunk("STRN");
            var joints = new Dictionary<string, JointSample[]>(StringComparer.OrdinalIgnoreCase);
            var xf = IffNode.Form(version.Children, "XFRM");
            foreach (var channel in xf.Children.Where(n => n.Tag == "XFIN"))
            {
                var b = channel.Data;
                int end = Array.IndexOf(b, (byte)0);
                int p = end + 1;
                if (end < 0 || p + 10 > b.Length)
                    throw new InvalidDataException("Animation transform record");
                bool animated = b[p++] != 0;
                int rotation = BitConverter.ToUInt16(b, p);
                p += 2;
                int mask = b[p++];
                var axes = new float[3][];
                for (int axis = 0; axis < 3; axis++, p += 2)
                {
                    int index = BitConverter.ToUInt16(b, p);
                    axes[axis] = (mask & (8 << axis)) != 0 ? translations[index] : Enumerable.Repeat(BitConverter.ToSingle(st, index * 4), lastFrame + 1).ToArray();
                }

                var frames = new JointSample[lastFrame + 1];
                for (int frame = 0; frame <= lastFrame; frame++)
                    frames[frame] = new JointSample
                    {
                        Rotation = animated ? rotations[rotation][frame] : staticRotations[rotation],
                        Translation = new Vector3(axes[0][frame], axes[1][frame], axes[2][frame])
                    };
                joints.Add(Encoding.ASCII.GetString(b, 0, end), frames);
            }

            var locomotion = version.Children.FirstOrDefault(n => n.Tag == "LOCT");
            float speed = locomotion == null ? 0 : BitConverter.ToSingle(locomotion.Data, 0);
            return new SkeletalAnimation(frameRate, lastFrame, speed, joints);
        }

        // The packed quaternion stores sign/magnitude X11,Y11,Z10. Format bytes describe
        // a base on an evenly spaced grid and its precision; positive W is reconstructed.
        static Quaternion Expand(uint packed, byte xf, byte yf, byte zf)
        {
            float x = Component(packed >> 21, xf, 10);
            float y = Component(packed >> 10, yf, 10);
            float z = Component(packed, zf, 9);
            float w = (float)Math.Sqrt(Math.Max(0, 1 - x * x - y * y - z * z));
            float length = (float)Math.Sqrt(w * w + x * x + y * y + z * z);
            return new Quaternion(x / length, y / length, z / length, w / length);
        }

        static float Component(uint value, byte format, int magnitudeBits)
        {
            int shift;
            // Prefixes FE, FC..FD, F8..FB, F0..F7, E0..EF, C0..DF, 80..BF.
            if (format >= 254)
                shift = 0;
            else if (format >= 252)
                shift = 1;
            else if (format >= 248)
                shift = 2;
            else if (format >= 240)
                shift = 3;
            else if (format >= 224)
                shift = 4;
            else if (format >= 192)
                shift = 5;
            else if (format >= 128)
                shift = 6;
            else
                throw new InvalidDataException("Quaternion compression format");
            int count = 1 << shift;
            float spacing = 2f / (count + 1);
            float basis = -1 + ((format & (count - 1)) + 1) * spacing;
            int sign = 1 << magnitudeBits;
            float delta = (value & (uint)(sign - 1)) * spacing / (sign - 1);
            return basis + ((value & (uint)sign) != 0 ? -delta : delta);
        }
    }
}
