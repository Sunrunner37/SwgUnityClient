using System;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>SKT (skeleton template) stores joint names, local parent indices, and bind transforms in a versioned SKTM form.</summary>
    public sealed class SktFile
    {
        public readonly struct Joint
        {
            public readonly string Name;
            public readonly int Parent;
            public readonly Vector3 Translation;
            public readonly Quaternion Rotation;
            public readonly Quaternion PreRotation;
            public readonly Quaternion PostRotation;

            internal Joint(string name, int parent, Vector3 translation, Quaternion rotation, Quaternion preRotation, Quaternion postRotation)
            {
                Name = name;
                Parent = parent;
                Translation = translation;
                Rotation = rotation;
                PreRotation = preRotation;
                PostRotation = postRotation;
            }
        }

        public Joint[] Joints { get; }
        SktFile(Joint[] joints) => Joints = joints;

        public static SktFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "SKTM") ?? throw new InvalidDataException("Expected SKTM skeleton.");
            var version = root.Children.First(node => node.Tag == "FORM");
            if (version.Kind != "0002")
                throw new NotSupportedException("Skeleton version " + version.Kind);
            var names = Encoding.ASCII.GetString(version.Chunk("NAME")).Split(new[] { '\0' }, StringSplitOptions.None).SkipLast(1).ToArray();
            var parents = version.Chunk("PRNT");
            var translations = version.Chunk("BPTR");
            var rotations = version.Chunk("BPRO");
            var pre = version.Chunk("RPRE");
            var post = version.Chunk("RPST");
            var joints = new Joint[names.Length];
            for (int i = 0; i < joints.Length; i++)
                joints[i] = new Joint(names[i], BitConverter.ToInt32(parents, i * 4),
                    new Vector3(BitConverter.ToSingle(translations, i * 12), BitConverter.ToSingle(translations, i * 12 + 4), BitConverter.ToSingle(translations, i * 12 + 8)),
                    Rotation(rotations, i * 16), Rotation(pre, i * 16), Rotation(post, i * 16));
            return new SktFile(joints);
        }

        // Native SKT quaternions store W first; Unity's constructor takes X, Y, Z, W.
        static Quaternion Rotation(byte[] bytes, int at) => new Quaternion(BitConverter.ToSingle(bytes, at + 4), BitConverter.ToSingle(bytes, at + 8), BitConverter.ToSingle(bytes, at + 12), BitConverter.ToSingle(bytes, at));
    }
}
