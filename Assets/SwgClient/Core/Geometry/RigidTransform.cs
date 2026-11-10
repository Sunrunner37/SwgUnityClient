using UnityEngine;

namespace Swg.Client.Core.Geometry
{
    // Value-only pose for decoded skeletons. It does not reference scene objects.
    internal struct RigidTransform
    {
        public Quaternion Rotation;
        public Vector3 Translation;
        public static RigidTransform Identity => new RigidTransform
        {
            Rotation = Quaternion.identity
        };

        public Vector3 Point(Vector3 point) => Rotation * point + Translation;
        public RigidTransform Inverse()
        {
            var rotation = Quaternion.Inverse(Rotation);
            return new RigidTransform
            {
                Rotation = rotation,
                Translation = rotation * -Translation
            };
        }

        public static RigidTransform operator *(RigidTransform a, RigidTransform b)
        {
            return new RigidTransform
            {
                Rotation = a.Rotation * b.Rotation,
                Translation = a.Point(b.Translation)
            };
        }
    }
}
