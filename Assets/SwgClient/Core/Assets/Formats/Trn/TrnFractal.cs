using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnFractal
    {
        public int Seed;
        public bool Bias;
        public float BiasValue;
        public bool Gain;
        public float GainValue;
        public int Octaves;
        public float FrequencyMultiplier;
        public float Amplitude;
        public float XFrequency;
        public float ZFrequency;
        public float XOffset;
        public float ZOffset;
        public int Combination;
        public static TrnFractal Parse(byte[] data) => new TrnFractal(data);
        TrnFractal(byte[] data)
        {
            var r = new ByteReader(data);
            Seed = unchecked((int)r.U32());
            Bias = r.U32() != 0;
            BiasValue = r.Float();
            Gain = r.U32() != 0;
            GainValue = r.Float();
            Octaves = checked((int)r.U32());
            if (Octaves < 1 || Octaves > 32)
                throw new System.IO.InvalidDataException("Invalid terrain fractal octave count");
            FrequencyMultiplier = r.Float();
            Amplitude = r.Float();
            XFrequency = r.Float();
            ZFrequency = r.Float();
            XOffset = r.Float();
            ZOffset = r.Float();
            Combination = checked((int)r.U32());
        }
    }
}
