using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Samples the original neutral breathing animation and skins MGN source vertices.
    /// This preview currently samples one pose, rather than running SWG's mood/state controller.</summary>
    public sealed class SkeletalPose
    {
        public const string NeutralIdleAnimation = "appearance/animation/all_b_idl_breathe_normally.ans";
        readonly List<Influence>[] weights;
        readonly RigidTransform[] skin;
        public string AnimationPath { get; private set; }

        public int PositionCount
        {
            get
            {
                return weights.Length;
            }
        }

        struct Influence
        {
            public int Joint;
            public float Weight;
        }

        SkeletalPose(List<Influence>[] weights, RigidTransform[] skin, string animation)
        {
            this.weights = weights;
            this.skin = skin;
            AnimationPath = animation;
        }

        public static SkeletalPose Load(TreAssetLoader loader, string satPath, string mgnPath)
        {
            var mgn = IffNode.Form(IffNode.Parse(loader.Read(mgnPath)), "SKMG");
            if (mgn == null)
                throw new InvalidDataException("Expected SKMG: " + mgnPath);
            var version = mgn.Children.First(n => n.Tag == "FORM");
            var transformNames = Strings(version.Chunk("XFNM"));
            var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bind = new List<RigidTransform>();
            var posed = new List<RigidTransform>();
            var samples = CompressedAnimationReader.ReadFirstFrame(loader.Read(NeutralIdleAnimation));
            var sat = IffNode.Form(IffNode.Parse(loader.Read(satPath)), "SMAT");
            if (sat == null)
                throw new InvalidDataException("Expected skeletal appearance: " + satPath);
            var satVersion = sat.Children.First(n => n.Tag == "FORM");
            var skeletons = Strings(satVersion.Chunk("SKTI"));
            for (int s = 0; s + 1 < skeletons.Length; s += 2)
            {
                var skt = IffNode.Form(IffNode.Parse(loader.Read(skeletons[s])), "SKTM");
                if (skt == null)
                    throw new InvalidDataException("Expected SKTM: " + skeletons[s]);
                var sktVersion = skt.Children.First(n => n.Tag == "FORM");
                if (sktVersion.Kind != "0002")
                    throw new NotSupportedException("Skeleton version " + sktVersion.Kind);
                var jointNames = Strings(sktVersion.Chunk("NAME"));
                var parents = sktVersion.Chunk("PRNT");
                var translations = sktVersion.Chunk("BPTR");
                var rotations = sktVersion.Chunk("BPRO");
                var pre = sktVersion.Chunk("RPRE");
                var post = sktVersion.Chunk("RPST");
                int offset = bind.Count;
                int attachment = -1;
                if (skeletons[s + 1].Length > 0 && !names.TryGetValue(skeletons[s + 1], out attachment))
                    throw new InvalidDataException("Unknown skeleton attachment " + skeletons[s + 1]);
                for (int j = 0; j < jointNames.Length; j++)
                {
                    int localParent = BitConverter.ToInt32(parents, j * 4);
                    int parent = localParent < 0 ? attachment : offset + localParent;
                    if (parent >= bind.Count)
                        throw new InvalidDataException("Skeleton parent order");
                    var t = Vector(translations, j * 12);
                    var r = ReadQuaternion(rotations, j * 16);
                    var before = ReadQuaternion(pre, j * 16);
                    var after = ReadQuaternion(post, j * 16);
                    var b = new RigidTransform
                    {
                        Rotation = after * (r * before),
                        Translation = t
                    };
                    JointSample sample;
                    var p = b;
                    if (samples.TryGetValue(jointNames[j], out sample))
                        p = new RigidTransform
                        {
                            Rotation = after * ((sample.Rotation * r) * before),
                            Translation = t + sample.Translation
                        };
                    bind.Add(parent < 0 ? b : bind[parent] * b);
                    posed.Add(parent < 0 ? p : posed[parent] * p);
                    names[jointNames[j]] = offset + j;
                }
            }

            var skin = new RigidTransform[transformNames.Length];
            for (int i = 0; i < skin.Length; i++)
            {
                int j;
                if (!names.TryGetValue(transformNames[i], out j))
                    throw new InvalidDataException("Missing skeleton joint " + transformNames[i]);
                skin[i] = posed[j] * bind[j].Inverse();
            }

            var counts = version.Chunk("TWHD");
            var data = version.Chunk("TWDT");
            var weights = new List<Influence>[counts.Length / 4];
            int at = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                int count = BitConverter.ToInt32(counts, i * 4);
                if (count < 0 || count > skin.Length || at + count * 8 > data.Length)
                    throw new InvalidDataException("MGN skin weights");
                weights[i] = new List<Influence>(count);
                for (int k = 0; k < count; k++, at += 8)
                {
                    int joint = BitConverter.ToInt32(data, at);
                    float w = BitConverter.ToSingle(data, at + 4);
                    if (joint < 0 || joint >= skin.Length || float.IsNaN(w) || w < 0)
                        throw new InvalidDataException("MGN influence");
                    weights[i].Add(new Influence { Joint = joint, Weight = w });
                }
            }

            if (at != data.Length)
                throw new InvalidDataException("MGN skin weight count mismatch");
            return new SkeletalPose(weights, skin, NeutralIdleAnimation);
        }

        public Vector3 TransformPosition(int sourcePositionIndex, Vector3 value)
        {
            return Transform(sourcePositionIndex, value, false);
        }

        // MGN normal indices differ from position indices. The position supplies the weights.
        public Vector3 TransformNormal(int sourcePositionIndex, Vector3 value)
        {
            return Transform(sourcePositionIndex, value, true);
        }

        Vector3 Transform(int index, Vector3 value, bool normal)
        {
            if (index < 0 || index >= weights.Length)
                throw new ArgumentOutOfRangeException("sourcePositionIndex");
            var influences = weights[index];
            if (influences.Count == 0)
                return value;
            var result = new Vector3();
            float total = 0;
            foreach (var w in influences)
            {
                var t = skin[w.Joint];
                result += (normal ? t.Rotation * value : t.Point(value)) * w.Weight;
                total += w.Weight;
            }

            if (total <= 0)
                return value;
            if (normal)
            {
                float length = result.magnitude;
                return length > 0 ? result / length : value;
            }

            return result / total;
        }

        static string[] Strings(byte[] b)
        {
            if (b == null)
                return new string[0];
            var values = new List<string>();
            int start = 0;
            for (int i = 0; i < b.Length; i++)
                if (b[i] == 0)
                {
                    values.Add(Encoding.ASCII.GetString(b, start, i - start));
                    start = i + 1;
                }

            return values.ToArray();
        }

        static Vector3 Vector(byte[] b, int p)
        {
            return new Vector3(BitConverter.ToSingle(b, p), BitConverter.ToSingle(b, p + 4), BitConverter.ToSingle(b, p + 8));
        }

        static Quaternion ReadQuaternion(byte[] b, int p)
        {
            // Native skeleton rotations are stored W, X, Y, Z.
            return new Quaternion(x: BitConverter.ToSingle(b, p + 4), y: BitConverter.ToSingle(b, p + 8), z: BitConverter.ToSingle(b, p + 12), w: BitConverter.ToSingle(b, p));
        }
    }
}
