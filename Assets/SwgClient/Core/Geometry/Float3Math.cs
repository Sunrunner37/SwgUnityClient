namespace Swg.Client.Core.Geometry
{
    internal static class Float3Math
    {
        internal static Float3 Add(Float3 a, Float3 b)
        {
            return new Float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        internal static Float3 Scale(Float3 a, float s)
        {
            return new Float3(a.X * s, a.Y * s, a.Z * s);
        }
    }
}
