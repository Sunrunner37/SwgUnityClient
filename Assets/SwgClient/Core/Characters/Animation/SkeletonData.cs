using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Native skeleton hierarchy, locomotion, and posture clips shared by body and equipment meshes.</summary>
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
            public readonly Dictionary<string, AshFile> Hierarchies = new Dictionary<string, AshFile>();
            public readonly Dictionary<string, SkeletonData> Skeletons = new Dictionary<string, SkeletonData>();
            public readonly Dictionary<string, SkeletalAnimation> Animations = new Dictionary<string, SkeletalAnimation>();
            public readonly Dictionary<string, Dictionary<string, LatFile.Animation>> AnimationStates = new Dictionary<string, Dictionary<string, LatFile.Animation>>();
        }

        static readonly ConditionalWeakTable<TreAssetLoader, AssetCache> caches = new ConditionalWeakTable<TreAssetLoader, AssetCache>();
        SkeletonAnimationResolver animationResolver;

        public SkeletalAnimation ResolveAction(string action, string weapon, CreaturePosture posture, bool combat = true, string performance = "") =>
            animationResolver.ResolveAction(action, weapon, posture, combat, performance);

        public SkeletalAnimation ResolveAmbient(string performance, string mood) => animationResolver.ResolveAmbient(performance, mood);

        public Joint[] Joints { get; private set; }
        public Matrix4x4[] BindPoses { get; private set; }
        public readonly Dictionary<string, SkeletalAnimation> RidingPoses = new Dictionary<string, SkeletalAnimation>(StringComparer.Ordinal);
        public SkeletalAnimation Idle { get; private set; }
        public SkeletalAnimation Walk { get; private set; }
        public string WalkAnimationPath { get; private set; }
        public SkeletalAnimation Run { get; private set; }
        public SkeletalAnimation Crawl { get; private set; }
        public string CrawlAnimationPath { get; private set; }
        public readonly Dictionary<CreaturePosture, SkeletalAnimation> Postures = new Dictionary<CreaturePosture, SkeletalAnimation>();
        internal readonly Dictionary<(CreaturePosture From, CreaturePosture To), SkeletalAnimation> Transitions = new Dictionary<(CreaturePosture, CreaturePosture), SkeletalAnimation>();
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
                    {
                        var bytes = loader.Read(path);
                        clip = Encoding.ASCII.GetString(bytes, 8, 4) == "KFAT" ? KeyframeAnimationReader.Read(bytes) : CompressedAnimationReader.Read(bytes);
                        cache.Animations.Add(path, clip);
                    }
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
                    var skt = SktFile.Parse(loader.Read(skeletons[s]));
                    int offset = joints.Count;
                    int attachment = -1;
                    if (skeletons[s + 1].Length > 0 && !names.TryGetValue(skeletons[s + 1], out attachment))
                        throw new InvalidDataException("Unknown skeleton attachment " + skeletons[s + 1]);
                    for (int j = 0; j < skt.Joints.Length; j++)
                    {
                        var source = skt.Joints[j];
                        int localParent = source.Parent;
                        int parent = localParent < 0 ? attachment : offset + localParent;
                        if (parent >= joints.Count)
                            throw new InvalidDataException("Skeleton parent order");
                        var joint = new Joint
                        {
                            Name = source.Name,
                            Parent = parent,
                            Translation = source.Translation,
                            Rotation = source.Rotation,
                            PreRotation = source.PreRotation,
                            PostRotation = source.PostRotation
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

                var result = new SkeletonData
                {
                    Joints = joints.ToArray(),
                    BindGlobals = bind.ToArray(),
                    BindPoses = bind.Select(b => Matrix4x4.TRS(b.Translation, b.Rotation, Vector3.one).inverse).ToArray()
                };
                LoadAnimations(loader, animationAppearance ?? satPath, Animation, result, cache);
                cache.Skeletons.Add(key, result);
                return result;
            }
        }

        static void LoadAnimations(TreAssetLoader loader, string satPath, Func<string, SkeletalAnimation> load, SkeletonData skeleton, AssetCache cache)
        {
            if (!cache.AnimationStates.TryGetValue(satPath, out var states))
            {
                states = new Dictionary<string, LatFile.Animation>(StringComparer.Ordinal);
                foreach (string table in IffNode.References(loader.Read(satPath), "appearance/", ".lat"))
                    foreach (var entry in LatFile.Parse(loader.Read(table)).Animations)
                        if (!states.ContainsKey(entry.Key))
                            states.Add(entry.Key, entry.Value);
                cache.AnimationStates.Add(satPath, states);
            }
            string hierarchyPath = states.ContainsKey("loop_standing") ? "appearance/ash/all_b.ash" : "appearance/ash/creature_base.ash";
            if (!cache.Hierarchies.TryGetValue(hierarchyPath, out var hierarchy))
                cache.Hierarchies.Add(hierarchyPath, hierarchy = AshFile.Parse(loader.Read(hierarchyPath)));
            skeleton.animationResolver = new SkeletonAnimationResolver(hierarchy, states, load, cache);
            if (states.TryGetValue("loop_riding", out var riding))
                foreach (var selection in riding.Selections)
                {
                    if (selection.Key.Selector != "rider_pose")
                        continue;
                    string path = selection.Value.FirstOrDefault();
                    if (path != null)
                        skeleton.RidingPoses.Add(selection.Key.Value, load(path));
                }
            string Clip(string state) => states.TryGetValue(state, out var clips) ? clips.Clips.FirstOrDefault() : null;
            var standing = new[] { "loop_ground", "loop_standing", "loop_stand", "loop_hovering" }
                .Where(states.ContainsKey).Select(name => states[name]).ToArray();
            skeleton.IdleAnimationPath = standing.SelectMany(state => state.IdleClips).FirstOrDefault()
                ?? Clip("loop_ground") ?? Clip("loop_stand") ?? SkeletalPose.NeutralIdleAnimation;
            var locomotion = standing.SelectMany(state => state.LocomotionClips).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => (Path: path, Clip: load(path))).Where(item => item.Clip.LocomotionSpeed > .001f)
                .OrderBy(item => item.Clip.LocomotionSpeed).ToArray();
            skeleton.WalkAnimationPath = locomotion.FirstOrDefault().Path;
            skeleton.RunAnimationPath = locomotion.LastOrDefault().Path
                ?? (skeleton.IdleAnimationPath == SkeletalPose.NeutralIdleAnimation ? "appearance/animation/all_b_loc_run.ans" : skeleton.IdleAnimationPath);
            skeleton.WalkAnimationPath ??= skeleton.RunAnimationPath;
            skeleton.Idle = load(skeleton.IdleAnimationPath);
            skeleton.Walk = load(skeleton.WalkAnimationPath);
            skeleton.Run = load(skeleton.RunAnimationPath);
            skeleton.CrawlAnimationPath = states.TryGetValue("loop_prone", out var prone)
                ? prone.Clips.FirstOrDefault(path => path.Contains("_crawl")) : null;
            skeleton.Crawl = skeleton.CrawlAnimationPath == null ? null : load(skeleton.CrawlAnimationPath);
            PostureAnimationBindings.Apply(skeleton, Clip, load);
        }

        static string[] Strings(byte[] bytes) => Encoding.ASCII.GetString(bytes).Split(new[] { '\0' }, StringSplitOptions.None).SkipLast(1).ToArray();
    }
}
