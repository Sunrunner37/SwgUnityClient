using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    internal static class CompressedAnimationReader
    {
        internal static Dictionary<string, JointSample> ReadFirstFrame(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "CKAT");
            if (root == null)
                throw new NotSupportedException("Expected compressed keyframe animation");
            var version = root.Children.First(n => n.Tag == "FORM");
            if (version.Kind != "0001")
                throw new NotSupportedException("Animation version " + version.Kind);
            var animatedRotations = new List<QuaternionData>();
            var ar = IffNode.Form(version.Children, "AROT");
            if (ar != null)
                foreach (var channel in ar.Children.Where(n => n.Tag == "QCHN"))
                {
                    var b = channel.Data;
                    // QCHN: uint16 count, 3 format bytes, then uint16 frame / uint32 packed quaternion.
                    if (b.Length < 11 || BitConverter.ToUInt16(b, 0) == 0)
                        throw new InvalidDataException("Empty rotation track");
                    animatedRotations.Add(Expand(BitConverter.ToUInt32(b, 7), b[2], b[3], b[4]));
                }

            var staticRotations = new List<QuaternionData>();
            var sr = version.Chunk("SROT");
            if (sr != null)
                for (int i = 0; i + 7 <= sr.Length; i += 7)
                    staticRotations.Add(Expand(BitConverter.ToUInt32(sr, i + 3), sr[i], sr[i + 1], sr[i + 2]));
            var translations = new List<float>();
            var at = IffNode.Form(version.Children, "ATRN");
            if (at != null)
                foreach (var ch in at.Children.Where(n => n.Tag == "CHNL"))
                {
                    if (ch.Data.Length < 8)
                        throw new InvalidDataException("Empty translation track");
                    translations.Add(BitConverter.ToSingle(ch.Data, 4));
                }

            var st = version.Chunk("STRN") ?? new byte[0];
            var result = new Dictionary<string, JointSample>(StringComparer.OrdinalIgnoreCase);
            var xf = IffNode.Form(version.Children, "XFRM");
            foreach (var ch in xf.Children.Where(n => n.Tag == "XFIN"))
            {
                var b = ch.Data;
                int end = Array.IndexOf(b, (byte)0);
                int p = end + 1;
                if (end < 0 || p + 10 > b.Length)
                    throw new InvalidDataException("Animation transform record");
                bool animated = b[p++] != 0;
                int rotation = BitConverter.ToUInt16(b, p);
                p += 2;
                int mask = b[p++];
                var v = new float[3];
                for (int axis = 0; axis < 3; axis++, p += 2)
                {
                    int channel = BitConverter.ToUInt16(b, p);
                    // SWG channel flags are bits 3,4,5 for translation X,Y,Z.
                    v[axis] = (mask & (8 << axis)) != 0 ? translations[channel] : BitConverter.ToSingle(st, channel * 4);
                }

                result[Encoding.ASCII.GetString(b, 0, end)] = new JointSample
                {
                    Rotation = animated ? animatedRotations[rotation] : staticRotations[rotation],
                    Translation = new Float3(v[0], v[1], v[2])
                };
            }

            return result;
        }

        // The packed quaternion stores sign/magnitude X11,Y11,Z10. Format bytes describe
        // a base on an evenly spaced grid and its precision; positive W is reconstructed.
        static QuaternionData Expand(uint packed, byte xf, byte yf, byte zf)
        {
            float x = Component(packed >> 21, xf, 10);
            float y = Component(packed >> 10, yf, 10);
            float z = Component(packed, zf, 9);
            float w = (float)Math.Sqrt(Math.Max(0, 1 - x * x - y * y - z * z));
            float length = (float)Math.Sqrt(w * w + x * x + y * y + z * z);
            return new QuaternionData(w / length, x / length, y / length, z / length);
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
