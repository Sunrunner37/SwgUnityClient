using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Native joint channels resampled at their authored frame rate. Locomotion stays in the movement controller.</summary>
    public sealed class SkeletalAnimation
    {
        public readonly float FrameRate;
        public readonly int LastFrame;
        public readonly float LocomotionSpeed;
        readonly Dictionary<string, JointSample[]> joints;
        public float Duration => LastFrame / FrameRate;

        internal SkeletalAnimation(float frameRate, int lastFrame, float locomotionSpeed, Dictionary<string, JointSample[]> joints)
        {
            FrameRate = frameRate;
            LastFrame = lastFrame;
            LocomotionSpeed = locomotionSpeed;
            this.joints = joints;
        }

        public JointSample Sample(string joint, float seconds, bool loop = true)
        {
            if (!joints.TryGetValue(joint, out var frames))
                return new JointSample
                {
                    Rotation = Quaternion.identity
                };
            float frame = Duration > 0 ? (loop ? Mathf.Repeat(seconds, Duration) : Mathf.Clamp(seconds, 0, Duration)) * FrameRate : 0;
            int first = Mathf.Min((int)frame, LastFrame);
            int second = Mathf.Min(first + 1, LastFrame);
            return new JointSample
            {
                Rotation = Quaternion.Slerp(frames[first].Rotation, frames[second].Rotation, frame - first),
                Translation = Vector3.Lerp(frames[first].Translation, frames[second].Translation, frame - first)
            };
        }
    }
}
