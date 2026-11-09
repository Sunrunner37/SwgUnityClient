namespace Swg.Client.Core.Audio
{
    public sealed class PcmData
    {
        public float[] Samples;
        public int Rate;
        public int Channels;
        public double Duration => (double)Samples.Length / Rate / Channels;
    }
}
