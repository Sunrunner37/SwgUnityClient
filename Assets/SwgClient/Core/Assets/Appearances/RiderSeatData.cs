using UnityEngine;

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>The native driver hardpoint and rider-pose selector from the mount tables.</summary>
    public readonly struct RiderSeatData
    {
        public readonly Matrix4x4 Transform;
        public readonly string PoseName;
        public RiderSeatData(Matrix4x4 transform, string poseName)
        {
            Transform = transform;
            PoseName = poseName;
        }
    }
}