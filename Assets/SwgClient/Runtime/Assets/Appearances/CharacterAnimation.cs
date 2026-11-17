using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Core.Characters.Animation;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Samples native locomotion and posture transitions into Unity bones; meshes remain GPU-skinned.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimation : MonoBehaviour
    {
        sealed class Rig
        {
            public SkeletonData Skeleton;
            public Transform[] Bones;
            public Pose[] StartPose;
            public readonly List<SkeletalAnimation> Transitions = new List<SkeletalAnimation>();
            public int TransitionIndex;
            public float PostureTime;
            public SkeletalAnimation Action;
            public SkeletalAnimation CombatIdle;
            public SkeletalAnimation Ambient;
        }

        readonly List<Rig> rigs = new List<Rig>();
        Vector3 previousPosition;
        bool positioned;
        string ridingPose;
        float idleTime;
        float locomotionTime;
        float postureAge = 1;
        float actionAge;
        float actionDuration;
        float actionPlaybackRate = 1;
        float? requestedActionDuration;
        public int ActionId { get; private set; }
        public float ActionTime => actionAge;
        public float ActionPlaybackRate => actionPlaybackRate;
        Coroutine actionLoading;
        Coroutine stanceLoading;
        Coroutine ambientLoading;
        public void SetAmbient(TreRuntimeAssets assets, string performance, string mood)
        {
            if (ambientLoading != null) StopCoroutine(ambientLoading);
            foreach (var rig in rigs) rig.Ambient = null;
            if (performance.Length > 0 || mood.Length > 0)
                ambientLoading = StartCoroutine(LoadAmbient(assets, performance, mood));
        }
        IEnumerator LoadAmbient(TreRuntimeAssets assets, string performance, string mood)
        {
            var ambientRigs = rigs.ToArray();
            var read = assets.ReadAsync(_ => ambientRigs.Select(rig => rig.Skeleton.ResolveAmbient(performance, mood)).ToArray());
            yield return new WaitUntil(() => read.IsCompleted);
            if (read.IsFaulted) { Debug.LogException(read.Exception.GetBaseException(), this); yield break; }
            for (int i = 0; i < ambientRigs.Length; i++) ambientRigs[i].Ambient = read.Result[i];
            idleTime = 0;
            foreach (var rig in ambientRigs) rig.PostureTime = 0;
        }
        bool actionPending;
        bool UnableToAct => Posture == CreaturePosture.Dead || Posture == CreaturePosture.Incapacitated || Posture == CreaturePosture.KnockedDown;
        public bool IsPlayingAction => actionAge < actionDuration;
        public string LastAction { get; private set; }
        public event System.Action<SkeletalAnimation> ActionStarted;
        public bool PlayAction(TreRuntimeAssets assets, string name, string weapon, bool reaction, float? durationSeconds = null, string performance = "")
        {
            if (UnableToAct || reaction && (actionPending || IsPlayingAction)) return false;
            if (actionLoading != null) StopCoroutine(actionLoading);
            ActionId++;
            requestedActionDuration = durationSeconds;
            actionLoading = StartCoroutine(LoadAction(assets, name, weapon, performance));
            return true;
        }
        internal IEnumerator LoadAction(TreRuntimeAssets assets, string name, string weapon, string performance = "")
        {
            var posture = Posture;
            var actionRigs = rigs.ToArray();
            actionPending = true;
            var read = assets.ReadAsync(_ => actionRigs.Select(rig => rig.Skeleton.ResolveAction(name, weapon, posture, performance: performance)).ToArray());
            yield return new WaitUntil(() => read.IsCompleted);
            actionPending = false;
            if (Posture != posture || UnableToAct) yield break;
            if (read.IsFaulted) { Debug.LogException(read.Exception.GetBaseException(), this); yield break; }
            if (read.Result.All(clip => clip == null)) { Debug.LogWarning("No native animation for " + name, this); yield break; }
            for (int i = 0; i < actionRigs.Length; i++) actionRigs[i].Action = read.Result[i];
            actionAge = 0;
            actionDuration = read.Result.Where(clip => clip != null).Max(clip => clip.Duration);
            actionPlaybackRate = 1;
            if (requestedActionDuration.HasValue) SetActionDuration(requestedActionDuration.Value);
            LastAction = name;
            ActionStarted?.Invoke(read.Result.First(clip => clip != null));
        }
        /// <summary>Uses the full server attack interval; asset and network latency must not accelerate the clip.</summary>
        public void SetActionDuration(float durationSeconds)
        {
            requestedActionDuration = durationSeconds;
            if (IsPlayingAction)
                actionPlaybackRate = actionDuration / Mathf.Max(.01f, durationSeconds);
        }
        public void SetCombatStance(TreRuntimeAssets assets, string weapon, bool combat)
        {
            if (stanceLoading != null) StopCoroutine(stanceLoading);
            if (!combat || UnableToAct)
            {
                foreach (var rig in rigs) rig.CombatIdle = null;
                return;
            }
            stanceLoading = StartCoroutine(LoadStance(assets, weapon));
        }
        internal IEnumerator LoadStance(TreRuntimeAssets assets, string weapon)
        {
            var posture = Posture;
            var stanceRigs = rigs.ToArray();
            var read = assets.ReadAsync(_ => stanceRigs.Select(rig => rig.Skeleton.ResolveAction(null, weapon, posture)).ToArray());
            yield return new WaitUntil(() => read.IsCompleted);
            if (Posture != posture || UnableToAct) yield break;
            if (read.IsFaulted) { Debug.LogException(read.Exception.GetBaseException(), this); yield break; }
            for (int i = 0; i < stanceRigs.Length; i++) stanceRigs[i].CombatIdle = read.Result[i];
        }
        void StopCombatAnimation()
        {
            if (actionLoading != null) StopCoroutine(actionLoading);
            if (stanceLoading != null) StopCoroutine(stanceLoading);
            actionLoading = null;
            stanceLoading = null;
            actionPending = false;
            ActionId++;
            requestedActionDuration = null;
            actionPlaybackRate = 1;
            actionDuration = 0;
            foreach (var rig in rigs) { rig.Action = null; rig.CombatIdle = null; }
        }
        void OnDisable() => StopCombatAnimation();
        public CreaturePosture Posture { get; private set; } = CreaturePosture.Upright;
        [Tooltip("Infer locomotion from world movement. Disable for UI previews, which always idle.")]
        public bool detectMovement = true;
        public bool IsChangingPosture => rigs.Exists(rig => rig.TransitionIndex < rig.Transitions.Count);
        public float Speed { get; private set; }
        public float LocomotionBlend { get; private set; }
        public float WalkRunBlend { get; private set; }
        public float LocomotionTime => locomotionTime;

        public Transform[] GetBones(SkeletonData skeleton)
        {
            foreach (var rig in rigs)
                if (ReferenceEquals(rig.Skeleton, skeleton))
                    return rig.Bones;
            var bones = new Transform[skeleton.Joints.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                var joint = skeleton.Joints[i];
                bones[i] = new GameObject(joint.Name).transform;
                bones[i].SetParent(joint.Parent < 0 ? transform : bones[joint.Parent], false);
                var pose = joint.Sample(skeleton.Idle.Sample(joint.Name, 0));
                bones[i].SetLocalPositionAndRotation(pose.position, pose.rotation);
            }

            rigs.Add(new Rig { Skeleton = skeleton, Bones = bones, StartPose = new Pose[bones.Length] });
            return bones;
        }

        public void SetRidingPose(string pose)
        {
            if (ridingPose == pose) return;
            ridingPose = pose;
            SetPosture(Posture, true);
        }

        public void SetPosture(CreaturePosture posture, bool snap = false)
        {
            if (Posture == posture && !snap)
                return;
            var previous = Posture;
            Posture = posture;
            StopCombatAnimation();
            locomotionTime = 0;
            LocomotionBlend = 0;
            postureAge = snap ? 1 : 0;
            foreach (var rig in rigs)
            {
                rig.Transitions.Clear();
                rig.TransitionIndex = 0;
                rig.PostureTime = 0;
                for (int i = 0; i < rig.Bones.Length; i++)
                    rig.StartPose[i] = new Pose(rig.Bones[i].localPosition, rig.Bones[i].localRotation);
                if (snap)
                    continue;
                void Add(CreaturePosture from, CreaturePosture to)
                {
                    if (rig.Skeleton.Transitions.TryGetValue((from, to), out var clip))
                        rig.Transitions.Add(clip);
                }
                if (rig.Skeleton.Transitions.ContainsKey((previous, posture)))
                    Add(previous, posture);
                else
                {
                    // Native tables bridge sitting to kneeling/prone through standing.
                    Add(previous, CreaturePosture.Upright);
                    Add(CreaturePosture.Upright, posture);
                }
            }
            Advance(0, 0);
        }

        void LateUpdate()
        {
            var position = transform.position;
            var delta = Vector3.ProjectOnPlane(position - previousPosition, Vector3.up);
            float speed = detectMovement && positioned && Time.deltaTime > 0 && delta.sqrMagnitude < 32 * 32 ? delta.magnitude / Time.deltaTime : 0;
            previousPosition = position;
            positioned = true;
            Advance(speed, Time.deltaTime);
        }

        SkeletalAnimation Locomotion(SkeletonData skeleton) => Posture == CreaturePosture.Prone ? skeleton.Crawl : skeleton.Run;

        internal void Advance(float speed, float seconds)
        {
            Speed = speed;
            actionAge += seconds * actionPlaybackRate;
            idleTime += seconds;
            postureAge += seconds;
            LocomotionBlend = Mathf.MoveTowards(LocomotionBlend, ridingPose == null && (Posture == CreaturePosture.Upright || Posture == CreaturePosture.Prone) && speed > .05f ? 1 : 0, seconds / .12f);
            // All equipment shares the same clock and native locomotion mapping.
            if (rigs.Count > 0 && speed > 0)
            {
                var clip = Locomotion(rigs[0].Skeleton);
                var skeleton = rigs[0].Skeleton;
                float scale = Mathf.Abs(transform.lossyScale.x);
                float authoredSpeed = (clip?.LocomotionSpeed ?? 0) * scale;
                if (Posture == CreaturePosture.Prone)
                {
                    if (authoredSpeed > .001f)
                        locomotionTime += seconds * speed / authoredSpeed;
                }
                else if (clip != null)
                {
                    var walk = skeleton.Walk ?? clip;
                    float walkSpeed = walk.LocomotionSpeed * scale;
                    float targetBlend = authoredSpeed > walkSpeed + .001f
                        ? Mathf.InverseLerp(walkSpeed, authoredSpeed, speed) : 1;
                    WalkRunBlend = Mathf.MoveTowards(WalkRunBlend, targetBlend, seconds / .15f);
                    // Blend by stride length and share normalized phase so walk/run and equipment stay in step.
                    float stride = Mathf.Lerp(walkSpeed * walk.Duration, authoredSpeed * clip.Duration, WalkRunBlend);
                    locomotionTime += stride > .001f ? seconds * speed * clip.Duration / stride : seconds;
                }
            }

            foreach (var rig in rigs)
            {
                rig.PostureTime += seconds;
                while (rig.TransitionIndex < rig.Transitions.Count && rig.PostureTime >= rig.Transitions[rig.TransitionIndex].Duration)
                {
                    rig.PostureTime -= rig.Transitions[rig.TransitionIndex].Duration;
                    rig.TransitionIndex++;
                }
                var transition = rig.TransitionIndex < rig.Transitions.Count ? rig.Transitions[rig.TransitionIndex] : null;
                var idleClip = ridingPose != null && rig.Skeleton.RidingPoses.TryGetValue(ridingPose, out var ridingClip) ? ridingClip
                    : rig.CombatIdle != null ? rig.CombatIdle
                    : rig.Ambient != null && (Posture == CreaturePosture.Upright || Posture == CreaturePosture.SkillAnimating) ? rig.Ambient
                    : rig.Skeleton.Postures.TryGetValue(Posture, out var postureClip) ? postureClip : rig.Skeleton.Idle;
                for (int i = 0; i < rig.Bones.Length; i++)
                {
                    var joint = rig.Skeleton.Joints[i];
                    var sample = transition != null ? transition.Sample(joint.Name, rig.PostureTime, false)
                        : idleClip.Sample(joint.Name, Posture == CreaturePosture.Upright ? idleTime : rig.PostureTime);
                    if (transition == null && ridingPose == null && (Posture == CreaturePosture.Upright || Posture == CreaturePosture.Prone))
                    {
                        var clip = Locomotion(rig.Skeleton);
                        var moving = clip == null || ReferenceEquals(clip, idleClip) ? sample : clip.Sample(joint.Name, locomotionTime);
                        if (Posture == CreaturePosture.Upright && clip != null && rig.Skeleton.Walk != null && clip.Duration > 0)
                        {
                            var walk = rig.Skeleton.Walk;
                            var walking = walk.Sample(joint.Name, locomotionTime / clip.Duration * walk.Duration);
                            moving = new JointSample
                            {
                                Rotation = Quaternion.Slerp(walking.Rotation, moving.Rotation, WalkRunBlend),
                                Translation = Vector3.Lerp(walking.Translation, moving.Translation, WalkRunBlend)
                            };
                        }
                        sample = new JointSample
                        {
                            Rotation = Quaternion.Slerp(sample.Rotation, moving.Rotation, LocomotionBlend),
                            Translation = Vector3.Lerp(sample.Translation, moving.Translation, LocomotionBlend)
                        };
                    }
                    if (rig.Action != null && IsPlayingAction)
                    {
                        var action = rig.Action.Sample(joint.Name, actionAge, false);
                        float weight = Mathf.Min(Mathf.Clamp01(actionAge / .1f), Mathf.Clamp01((actionDuration - actionAge) / .12f));
                        sample = new JointSample
                        {
                            Rotation = Quaternion.Slerp(sample.Rotation, action.Rotation, weight),
                            Translation = Vector3.Lerp(sample.Translation, action.Translation, weight)
                        };
                    }
                    var pose = joint.Sample(sample);
                    // Native mounted creatures use ZeroTranslationTransformModifier on the root joint.
                    if (ridingPose != null && joint.Name == "root")
                        pose.position = Vector3.zero;
                    float blend = Mathf.Clamp01(postureAge / .12f);
                    rig.Bones[i].SetLocalPositionAndRotation(Vector3.Lerp(rig.StartPose[i].position, pose.position, blend),
                        Quaternion.Slerp(rig.StartPose[i].rotation, pose.rotation, blend));
                }
            }
        }
    }
}
