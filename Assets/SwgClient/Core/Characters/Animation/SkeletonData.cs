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
        AssetCache assetCache;
        Dictionary<string, LatFile.Animation> logicalAnimations;
        AshFile hierarchy;
        Func<string, SkeletalAnimation> loadAnimation;

        public SkeletalAnimation ResolveAction(string action, string weapon, CreaturePosture posture, bool combat = true)
        {
            lock (assetCache)
            {
                string postureState = posture == CreaturePosture.Prone ? "prone" : posture == CreaturePosture.Crouched ? "kneeling"
                    : posture == CreaturePosture.Dead ? "dead" : posture == CreaturePosture.Incapacitated ? "incapacitated_face_up" : "";
                foreach (string candidate in ActionCandidates(action))
                {
                    string name = hierarchy.Resolve(candidate, weapon, combat ? "combat" : "", postureState);
                    if (candidate != null && name == "default") continue;
                    if (!logicalAnimations.TryGetValue(name, out var logical))
                    {
                        if (candidate == null || !logicalAnimations.TryGetValue("cbt_" + candidate, out logical) && !logicalAnimations.TryGetValue("rea_" + candidate, out logical))
                            continue;
                    }
                    string path = (action == null ? logical.IdleClips.FirstOrDefault() : null) ?? logical.Clips.FirstOrDefault();
                    if (path != null) return loadAnimation(path);
                }
                return null;
            }
        }

        static IEnumerable<string> ActionCandidates(string action)
        {
            yield return action;
            if (action == null) yield break;
            // Core3 can request a fourth variant and a center-leg strike that older native hierarchies omit.
            // Prefer another native variant at the same height, then the adjacent leg's animation.
            var directions = action.Contains("_low_center") ? new[] { action, action.Replace("_low_center", "_low_left"), action.Replace("_low_center", "_low_right") } : new[] { action };
            foreach (string direction in directions)
            {
                if (direction != action) yield return direction;
                if (direction.StartsWith("attack_") && char.IsDigit(direction[direction.Length - 1]))
                    for (int i = 0; i < 3; i++) yield return direction.Substring(0, direction.Length - 1) + i;
            }
            if (action.StartsWith("get_hit_"))
                yield return action.StartsWith("get_hit_medium") ? "get_hit_medium" : "get_hit_light";
            if (action == "dodge")
            {
                yield return "unarmed_standing_ready_dodge_bob_left";
                yield return "rea_stand_combat_dodge";
                yield return "rea_stand_dodge";
            }
            if (action == "fire_single") yield return "fire_1";
            if (action == "fire_burst") yield return "fire_3";
            if (action == "fire_suppress") yield return "fire_5";
        }

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
            skeleton.assetCache = cache;
            skeleton.hierarchy = hierarchy;
            skeleton.logicalAnimations = states;
            skeleton.loadAnimation = load;
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
            var postures = new[]
            {
                (Posture: CreaturePosture.Upright, State: "standing"),
                (Posture: CreaturePosture.Crouched, State: "kneeling"),
                (Posture: CreaturePosture.Prone, State: "prone"),
                (Posture: CreaturePosture.Sitting, State: "sitting_ground")
            };
            string fallen = Clip("loop_incapacitated_face_up") ?? Clip("loop_incapacitated");
            if (fallen != null)
                foreach (var posture in new[] { CreaturePosture.Incapacitated, CreaturePosture.Dead, CreaturePosture.KnockedDown })
                {
                    skeleton.Postures.Add(posture, load(fallen));
                    string fall = Clip("trn_combat_standing_hit_to_incapacitated_face_up");
                    if (fall != null) skeleton.Transitions.Add((CreaturePosture.Upright, posture), load(fall));
                }
            skeleton.Postures.Add(CreaturePosture.Upright, skeleton.Idle);
            foreach (var posture in postures)
            {
                string idle = Clip("loop_" + posture.State);
                if (posture.Posture != CreaturePosture.Upright && idle != null)
                    skeleton.Postures.Add(posture.Posture, load(idle));
                foreach (var next in postures)
                {
                    string transition = Clip("trn_" + posture.State + "_to_" + next.State);
                    if (transition != null)
                        skeleton.Transitions.Add((posture.Posture, next.Posture), load(transition));
                }
            }
        }

        static Quaternion Rotation(byte[] bytes, int at) => new Quaternion(BitConverter.ToSingle(bytes, at + 4), BitConverter.ToSingle(bytes, at + 8), BitConverter.ToSingle(bytes, at + 12), BitConverter.ToSingle(bytes, at));
        static string[] Strings(byte[] bytes) => Encoding.ASCII.GetString(bytes).Split(new[] { '\0' }, StringSplitOptions.None).SkipLast(1).ToArray();
    }
}
