using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Characters.Animation;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>KFAT stores uncompressed skeletal keyframes, using 32-bit frame/channel indices and WXYZ quaternions.</summary>
    public static class KeyframeAnimationReader
    {
        public static SkeletalAnimation Read(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "KFAT") ?? throw new InvalidDataException("Expected KFAT animation");
            var version = root.Children.First(n => n.Tag == "FORM");
            if (version.Kind != "0003") throw new NotSupportedException("KFAT version " + version.Kind);
            var info = new ByteReader(version.Chunk("INFO"));
            float rate = info.Float();
            int lastFrame = checked((int)info.U32());
            if (rate <= 0) throw new InvalidDataException("Invalid animation frame rate");
            var rotations = new List<Quaternion[]>();
            var rotationForm = IffNode.Form(version.Children, "AROT");
            if (rotationForm != null)
                foreach (var channel in rotationForm.Children.Where(n => n.Tag == "QCHN"))
                {
                    var reader = new ByteReader(channel.Data);
                    int count = reader.Count();
                    var times = new int[count];
                    var values = new Quaternion[count];
                    for (int i = 0; i < count; i++) { times[i] = checked((int)reader.U32()); values[i] = ReadQuaternion(reader); }
                    rotations.Add(Frames(times, values, lastFrame, Quaternion.Slerp));
                }
            var staticRotationReader = new ByteReader(version.Chunk("SROT"));
            var staticRotations = new List<Quaternion>();
            while (staticRotationReader.Remaining > 0) staticRotations.Add(ReadQuaternion(staticRotationReader));
            var translations = new List<float[]>();
            var translationForm = IffNode.Form(version.Children, "ATRN");
            if (translationForm != null)
                foreach (var channel in translationForm.Children.Where(n => n.Tag == "CHNL"))
                {
                    var reader = new ByteReader(channel.Data);
                    int count = reader.Count();
                    var times = new int[count];
                    var values = new float[count];
                    for (int i = 0; i < count; i++) { times[i] = checked((int)reader.U32()); values[i] = reader.Float(); }
                    translations.Add(Frames(times, values, lastFrame, Mathf.Lerp));
                }
            var staticTranslations = version.Chunk("STRN");
            var joints = new Dictionary<string, JointSample[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var channel in IffNode.Form(version.Children, "XFRM").Children.Where(n => n.Tag == "XFIN"))
            {
                var reader = new ByteReader(channel.Data);
                string name = reader.CString();
                bool animated = reader.Byte() != 0;
                int rotation = checked((int)reader.U32());
                uint mask = reader.U32();
                var axes = new float[3][];
                for (int axis = 0; axis < 3; axis++)
                {
                    int index = checked((int)reader.U32());
                    axes[axis] = (mask & (8u << axis)) != 0 ? translations[index] : Enumerable.Repeat(BitConverter.ToSingle(staticTranslations, index * 4), lastFrame + 1).ToArray();
                }
                var frames = new JointSample[lastFrame + 1];
                for (int frame = 0; frame <= lastFrame; frame++) frames[frame] = new JointSample { Rotation = animated ? rotations[rotation][frame] : staticRotations[rotation], Translation = new Vector3(axes[0][frame], axes[1][frame], axes[2][frame]) };
                joints.Add(name, frames);
            }
            var locomotion = version.Children.FirstOrDefault(n => n.Tag == "LOCT");
            return new SkeletalAnimation(rate, lastFrame, locomotion == null ? 0 : BitConverter.ToSingle(locomotion.Data, 0), joints);
        }
        static Quaternion ReadQuaternion(ByteReader reader)
        {
            float w = reader.Float();
            return new Quaternion(reader.Float(), reader.Float(), reader.Float(), w).normalized;
        }
        static T[] Frames<T>(int[] times, T[] values, int lastFrame, Func<T, T, float, T> interpolate)
        {
            if (times.Length == 0) throw new InvalidDataException("Empty animation channel");
            var frames = new T[lastFrame + 1];
            int key = 0;
            for (int frame = 0; frame <= lastFrame; frame++)
            {
                while (key + 1 < times.Length && times[key + 1] <= frame) key++;
                int next = Math.Min(key + 1, times.Length - 1);
                frames[frame] = interpolate(values[key], values[next], times[next] == times[key] ? 0 : Mathf.InverseLerp(times[key], times[next], frame));
            }
            return frames;
        }
    }
}
