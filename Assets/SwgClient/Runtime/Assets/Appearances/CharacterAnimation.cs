using System.Collections.Generic;
using Swg.Client.Core.Characters.Animation;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Samples native idle/run clips into Unity bones; meshes are skinned by Unity, not rebuilt every frame.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimation : MonoBehaviour
    {
        sealed class Rig
        {
            public SkeletonData Skeleton;
            public Transform[] Bones;
        }

        readonly List<Rig> rigs = new List<Rig>();
        Vector3 previousPosition;
        bool positioned;
        float idleTime;
        float runTime;
        [Tooltip("Infer locomotion from world movement. Disable for UI previews, which always idle.")]
        public bool detectMovement = true;
        public float Speed { get; private set; }
        public float RunBlend { get; private set; }
        public float RunTime => runTime;

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

            rigs.Add(new Rig { Skeleton = skeleton, Bones = bones });
            return bones;
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

        internal void Advance(float speed, float seconds)
        {
            Speed = speed;
            idleTime += seconds;
            RunBlend = Mathf.MoveTowards(RunBlend, speed > .05f ? 1 : 0, seconds / .12f);
            // All equipment shares the same clock and native locomotion mapping.
            if (rigs.Count > 0 && speed > 0)
            {
                float authoredSpeed = rigs[0].Skeleton.Run.LocomotionSpeed * Mathf.Abs(transform.lossyScale.x);
                if (authoredSpeed > .001f)
                    runTime += seconds * speed / authoredSpeed;
            }

            foreach (var rig in rigs)
                for (int i = 0; i < rig.Bones.Length; i++)
                {
                    var joint = rig.Skeleton.Joints[i];
                    var idle = rig.Skeleton.Idle.Sample(joint.Name, idleTime);
                    var run = rig.Skeleton.Run.Sample(joint.Name, runTime);
                    var sample = new JointSample
                    {
                        Rotation = Quaternion.Slerp(idle.Rotation, run.Rotation, RunBlend),
                        Translation = Vector3.Lerp(idle.Translation, run.Translation, RunBlend)
                    };
                    var pose = joint.Sample(sample);
                    rig.Bones[i].SetLocalPositionAndRotation(pose.position, pose.rotation);
                }
        }
    }
}
