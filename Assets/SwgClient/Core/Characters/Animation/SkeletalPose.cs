using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Builds the neutral preview pose and retains bind-space influences for animated world characters.</summary>
    public sealed class SkeletalPose
    {
        public const string NeutralIdleAnimation = "appearance/animation/all_b_idl_breathe_normally.ans";
        readonly List<Influence>[] weights;
        readonly RigidTransform[] skin;
        readonly int[] jointMap;
        readonly SkeletonData skeleton;
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

        SkeletalPose(List<Influence>[] weights, RigidTransform[] skin, string animation, SkeletonData skeleton, int[] jointMap)
        {
            this.weights = weights;
            this.skin = skin;
            AnimationPath = animation;
            this.skeleton = skeleton;
            this.jointMap = jointMap;
        }

        public static SkeletalPose Load(TreAssetLoader loader, string satPath, string mgnPath, string animationAppearance = null)
        {
            var mgn = IffNode.Form(IffNode.Parse(loader.Read(mgnPath)), "SKMG");
            if (mgn == null)
                throw new InvalidDataException("Expected SKMG: " + mgnPath);
            var version = mgn.Children.First(n => n.Tag == "FORM");
            var transformNames = Strings(version.Chunk("XFNM"));
            var skeleton = SkeletonData.Load(loader, satPath, animationAppearance);
            var posed = new RigidTransform[skeleton.Joints.Length];
            var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < skeleton.Joints.Length; j++)
            {
                var joint = skeleton.Joints[j];
                var pose = joint.Sample(skeleton.Idle.Sample(joint.Name, 0));
                var local = new RigidTransform
                {
                    Rotation = pose.rotation,
                    Translation = pose.position
                };
                posed[j] = joint.Parent < 0 ? local : posed[joint.Parent] * local;
                names[joint.Name] = j;
            }

            var skin = new RigidTransform[transformNames.Length];
            var jointMap = new int[transformNames.Length];
            for (int i = 0; i < skin.Length; i++)
            {
                if (!names.TryGetValue(transformNames[i], out int j))
                    throw new InvalidDataException("Missing skeleton joint " + transformNames[i]);
                skin[i] = posed[j] * skeleton.BindGlobals[j].Inverse();
                jointMap[i] = j;
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
            return new SkeletalPose(weights, skin, skeleton.IdleAnimationPath, skeleton, jointMap);
        }

        public SkeletalMeshData CreateSkin(Vector3[] vertices, Vector3[] normals, int[] sourceIndices)
        {
            var counts = new byte[vertices.Length];
            var mapped = new List<BoneWeight1>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var influences = weights[sourceIndices[i]].Where(w => w.Weight > 0).OrderByDescending(w => w.Weight).ToArray();
                if (influences.Length == 0)
                    throw new InvalidDataException("Unweighted skeletal vertex");
                counts[i] = checked((byte)influences.Length);
                float total = influences.Sum(w => w.Weight);
                foreach (var w in influences)
                    mapped.Add(new BoneWeight1 { boneIndex = jointMap[w.Joint], weight = w.Weight / total });
            }

            return new SkeletalMeshData
            {
                Skeleton = skeleton,
                Vertices = vertices,
                Normals = normals,
                BonesPerVertex = counts,
                Weights = mapped.ToArray()
            };
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
    }
}
