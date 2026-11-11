namespace Swg.Client.Runtime.World
{
    public readonly struct WorldPreparationProgress
    {
        public readonly string Phase;
        public readonly float Fraction;
        public WorldPreparationProgress(string phase, float fraction)
        {
            Phase = phase;
            Fraction = fraction;
        }
    }
}
