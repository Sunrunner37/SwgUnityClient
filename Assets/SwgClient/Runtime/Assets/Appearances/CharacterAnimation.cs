using System.Collections.Generic;
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
        }

        readonly List<Rig> rigs = new List<Rig>();
        Vector3 previousPosition;
        bool positioned;
        float idleTime;
        float locomotionTime;
        float postureAge = 1;
        public CreaturePosture Posture { get; private set; } = CreaturePosture.Upright;
        [Tooltip("Infer locomotion from world movement. Disable for UI previews, which always idle.")]
        public bool detectMovement = true;
        public bool IsChangingPosture => rigs.Exists(rig => rig.TransitionIndex < rig.Transitions.Count);
        public float Speed { get; private set; }
        public float LocomotionBlend { get; private set; }
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

        public void SetPosture(CreaturePosture posture, bool snap = false)
        {
            if (Posture == posture && !snap)
                return;
            var previous = Posture;
            Posture = posture;
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
            idleTime += seconds;
            postureAge += seconds;
            LocomotionBlend = Mathf.MoveTowards(LocomotionBlend, (Posture == CreaturePosture.Upright || Posture == CreaturePosture.Prone) && speed > .05f ? 1 : 0, seconds / .12f);
            // All equipment shares the same clock and native locomotion mapping.
            if (rigs.Count > 0 && speed > 0)
            {
                var clip = Locomotion(rigs[0].Skeleton);
                float authoredSpeed = (clip?.LocomotionSpeed ?? 0) * Mathf.Abs(transform.lossyScale.x);
                if (authoredSpeed > .001f)
                    locomotionTime += seconds * speed / authoredSpeed;
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
                var idleClip = rig.Skeleton.Postures.TryGetValue(Posture, out var postureClip) ? postureClip : rig.Skeleton.Idle;
                for (int i = 0; i < rig.Bones.Length; i++)
                {
                    var joint = rig.Skeleton.Joints[i];
                    var sample = transition != null ? transition.Sample(joint.Name, rig.PostureTime, false)
                        : idleClip.Sample(joint.Name, Posture == CreaturePosture.Upright ? idleTime : rig.PostureTime);
                    if (transition == null && (Posture == CreaturePosture.Upright || Posture == CreaturePosture.Prone))
                    {
                        var clip = Locomotion(rig.Skeleton);
                        var moving = clip == null || ReferenceEquals(clip, idleClip) ? sample : clip.Sample(joint.Name, locomotionTime);
                        sample = new JointSample
                        {
                            Rotation = Quaternion.Slerp(sample.Rotation, moving.Rotation, LocomotionBlend),
                            Translation = Vector3.Lerp(sample.Translation, moving.Translation, LocomotionBlend)
                        };
                    }
                    var pose = joint.Sample(sample);
                    float blend = Mathf.Clamp01(postureAge / .12f);
                    rig.Bones[i].SetLocalPositionAndRotation(Vector3.Lerp(rig.StartPose[i].position, pose.position, blend),
                        Quaternion.Slerp(rig.StartPose[i].rotation, pose.rotation, blend));
                }
            }
        }
    }
}
