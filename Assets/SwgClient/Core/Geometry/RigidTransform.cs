namespace Swg.Client.Core.Geometry
{
    internal struct RigidTransform
    {
        public QuaternionData Rotation;
        public Float3 Translation;
        public static RigidTransform Identity
        {
            get
            {
                return new RigidTransform
                {
                    Rotation = QuaternionData.Identity
                };
            }
        }

        public Float3 Point(Float3 p)
        {
            return Float3Math.Add(Rotation.Rotate(p), Translation);
        }

        public RigidTransform Inverse()
        {
            var r = Rotation.Inverse();
            return new RigidTransform
            {
                Rotation = r,
                Translation = r.Rotate(Float3Math.Scale(Translation, -1))
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
