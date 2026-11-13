using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;
using UnityEngine;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Native skeleton hierarchy and its standing locomotion clips, shared by body and equipment meshes.</summary>
    public sealed class SkeletonData
    {
        public struct Joint
        {
            public string Name;
            public int Parent;
            public Vector3 Translation;
            public Quaternion Rotation;
            public Quaternion PreRotation;
            public Quaternion PostRotation;
            public Pose Sample(JointSample animation) => new Pose(Translation + animation.Translation, PostRotation * ((animation.Rotation * Rotation) * PreRotation));
        }

        sealed class AssetCache
        {
            public readonly Dictionary<string, SkeletonData> Skeletons = new Dictionary<string, SkeletonData>();
            public readonly Dictionary<string, SkeletalAnimation> Animations = new Dictionary<string, SkeletalAnimation>();
        }

        static readonly ConditionalWeakTable<TreAssetLoader, AssetCache> caches = new ConditionalWeakTable<TreAssetLoader, AssetCache>();
        public Joint[] Joints { get; private set; }
        public Matrix4x4[] BindPoses { get; private set; }
        public SkeletalAnimation Idle { get; private set; }
        public SkeletalAnimation Run { get; private set; }
        public string IdleAnimationPath { get; private set; }
        public string RunAnimationPath { get; private set; }
        internal RigidTransform[] BindGlobals { get; private set; }

        public static SkeletonData Load(TreAssetLoader loader, string satPath, string animationAppearance = null)
        {
            var cache = caches.GetValue(loader, _ => new AssetCache());
            lock (cache)
            {
                string key = satPath + "|" + (animationAppearance ?? satPath);
                if (cache.Skeletons.TryGetValue(key, out var existing))
                    return existing;
                SkeletalAnimation Animation(string path)
                {
                    if (!cache.Animations.TryGetValue(path, out var clip))
                        cache.Animations.Add(path, clip = CompressedAnimationReader.Read(loader.Read(path)));
                    return clip;
                }

                var sat = IffNode.Form(IffNode.Parse(loader.Read(satPath)), "SMAT") ?? throw new InvalidDataException("Expected skeletal appearance: " + satPath);
                var version = sat.Children.First(n => n.Tag == "FORM");
                var skeletons = Strings(version.Chunk("SKTI"));
                var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var joints = new List<Joint>();
                var bind = new List<RigidTransform>();
                for (int s = 0; s + 1 < skeletons.Length; s += 2)
                {
                    var skt = IffNode.Form(IffNode.Parse(loader.Read(skeletons[s])), "SKTM") ?? throw new InvalidDataException("Expected skeleton: " + skeletons[s]);
                    var sktVersion = skt.Children.First(n => n.Tag == "FORM");
                    if (sktVersion.Kind != "0002")
                        throw new NotSupportedException("Skeleton version " + sktVersion.Kind);
                    var jointNames = Strings(sktVersion.Chunk("NAME"));
                    var parents = sktVersion.Chunk("PRNT");
                    var translations = sktVersion.Chunk("BPTR");
                    var rotations = sktVersion.Chunk("BPRO");
                    var pre = sktVersion.Chunk("RPRE");
                    var post = sktVersion.Chunk("RPST");
                    int offset = joints.Count;
                    int attachment = -1;
                    if (skeletons[s + 1].Length > 0 && !names.TryGetValue(skeletons[s + 1], out attachment))
                        throw new InvalidDataException("Unknown skeleton attachment " + skeletons[s + 1]);
                    for (int j = 0; j < jointNames.Length; j++)
                    {
                        int localParent = BitConverter.ToInt32(parents, j * 4);
                        int parent = localParent < 0 ? attachment : offset + localParent;
                        if (parent >= joints.Count)
                            throw new InvalidDataException("Skeleton parent order");
                        var joint = new Joint
                        {
                            Name = jointNames[j],
                            Parent = parent,
                            Translation = new Vector3(BitConverter.ToSingle(translations, j * 12), BitConverter.ToSingle(translations, j * 12 + 4), BitConverter.ToSingle(translations, j * 12 + 8)),
                            Rotation = Rotation(rotations, j * 16),
                            PreRotation = Rotation(pre, j * 16),
                            PostRotation = Rotation(post, j * 16)
                        };
                        var pose = joint.Sample(new JointSample { Rotation = Quaternion.identity });
                        var local = new RigidTransform
                        {
                            Rotation = pose.rotation,
                            Translation = pose.position
                        };
                        joints.Add(joint);
                        bind.Add(parent < 0 ? local : bind[parent] * local);
                        names[joint.Name] = joints.Count - 1;
                    }
                }

                string idlePath = IdlePath(loader, animationAppearance ?? satPath);
                string runPath = RunPath(loader, animationAppearance ?? satPath, idlePath);
                var result = new SkeletonData
                {
                    Joints = joints.ToArray(),
                    BindGlobals = bind.ToArray(),
                    BindPoses = bind.Select(b => Matrix4x4.TRS(b.Translation, b.Rotation, Vector3.one).inverse).ToArray(),
                    Idle = Animation(idlePath),
                    IdleAnimationPath = idlePath,
                    Run = Animation(runPath),
                    RunAnimationPath = runPath
                };
                cache.Skeletons.Add(key, result);
                return result;
            }
        }

        static string IdlePath(TreAssetLoader loader, string satPath)
        {
            // Non-humanoid appearances define their neutral state in the logical animation table.
            foreach (string table in IffNode.References(loader.Read(satPath), "appearance/", ".lat"))
            {
                var root = IffNode.Parse(loader.Read(table)).Single(n => n.Kind == "LATT");
                var version = root.Children.First(n => n.Tag == "FORM");
                foreach (string state in new[] { "loop_ground", "loop_stand" })
                {
                    var animation = version.Children.FirstOrDefault(n => n.Kind == "ANIM" && new ByteReader(n.Chunk("INFO")).CString() == state);
                    if (animation == null)
                        continue;
                    string path = IffNode.References(animation.Data, "appearance/animation/", ".ans").FirstOrDefault();
                    if (path != null)
                        return path;
                }
            }
            return SkeletalPose.NeutralIdleAnimation;
        }

        static string RunPath(TreAssetLoader loader, string satPath, string idlePath)
        {
            // Use the native standing locomotion mapping; the filename's gender is not a reliable selector.
            foreach (string table in IffNode.References(loader.Read(satPath), "appearance/", ".lat"))
            {
                var root = IffNode.Parse(loader.Read(table)).Single(n => n.Kind == "LATT");
                var version = root.Children.First(n => n.Tag == "FORM");
                var standing = version.Children.FirstOrDefault(n => n.Kind == "ANIM" && new ByteReader(n.Chunk("INFO")).CString() == "loop_standing");
                if (standing == null)
                    continue;
                var run = IffNode.References(standing.Data, "appearance/animation/", ".ans").FirstOrDefault(path => path.Contains("_loc_run"));
                if (run != null)
                    return run;
            }

            return idlePath == SkeletalPose.NeutralIdleAnimation ? "appearance/animation/all_b_loc_run.ans" : idlePath;
        }

        static Quaternion Rotation(byte[] bytes, int at) => new Quaternion(BitConverter.ToSingle(bytes, at + 4), BitConverter.ToSingle(bytes, at + 8), BitConverter.ToSingle(bytes, at + 12), BitConverter.ToSingle(bytes, at));
        static string[] Strings(byte[] bytes) => Encoding.ASCII.GetString(bytes).Split(new[] { '\0' }, StringSplitOptions.None).SkipLast(1).ToArray();
    }
}
