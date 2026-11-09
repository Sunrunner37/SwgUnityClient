namespace Swg.Client.Core.Geometry
{
    internal struct QuaternionData
    {
        public float W;
        public float X;
        public float Y;
        public float Z;
        public QuaternionData(float w, float x, float y, float z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        public static QuaternionData Identity
        {
            get
            {
                return new QuaternionData(1, 0, 0, 0);
            }
        }

        public QuaternionData Inverse()
        {
            return new QuaternionData(W, -X, -Y, -Z);
        }

        public static QuaternionData operator *(QuaternionData a, QuaternionData b)
        {
            return new QuaternionData(a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z, a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y, a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X, a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W);
        }

        public Float3 Rotate(Float3 v)
        {
            var t = new Float3(2 * (Y * v.Z - Z * v.Y), 2 * (Z * v.X - X * v.Z), 2 * (X * v.Y - Y * v.X));
            return new Float3(v.X + W * t.X + Y * t.Z - Z * t.Y, v.Y + W * t.Y + Z * t.X - X * t.Z, v.Z + W * t.Z + X * t.Y - Y * t.X);
        }
    }
}
