using System;
using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain
{
    // Seeded noise and octave composition used by Core3's MapFractal and PerlinNoise.
    public sealed class NativeTerrainFractal
    {
        readonly int[] permutation = new int[514];
        readonly float[] gradientX = new float[514];
        readonly float[] gradientY = new float[514];
        readonly bool bias;
        readonly float biasValue;
        readonly bool gain;
        readonly float gainValue;
        readonly int octaves;
        readonly float frequencyMultiplier;
        readonly float amplitude;
        readonly float xFrequency;
        readonly float zFrequency;
        readonly float xOffset;
        readonly float zOffset;
        readonly int combination;
        readonly float normalization;
        public NativeTerrainFractal(TrnFractal definition)
        {
            var random = new NativeRandom(definition.Seed);
            bias = definition.Bias;
            biasValue = definition.BiasValue;
            gain = definition.Gain;
            gainValue = definition.GainValue;
            octaves = definition.Octaves;
            frequencyMultiplier = definition.FrequencyMultiplier;
            amplitude = definition.Amplitude;
            xFrequency = definition.XFrequency;
            zFrequency = definition.ZFrequency;
            xOffset = definition.XOffset;
            zOffset = definition.ZOffset;
            combination = definition.Combination;
            float sum = 0;
            float weight = 1;
            for (int i = 0; i < octaves; i++)
            {
                sum += weight;
                weight *= amplitude;
            }

            normalization = 1 / sum;
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = i;
                random.Next(); // The original also generates an unused one-dimensional gradient.
                float x = (random.Next() % 512 - 256) / 256f;
                float y = (random.Next() % 512 - 256) / 256f;
                double length = Math.Sqrt(x * x + y * y);
                gradientX[i] = (float)(x / length);
                gradientY[i] = (float)(y / length);
            }

            for (int i = 255; i > 0; i--)
            {
                int j = random.Next() % 256;
                int value = permutation[i];
                permutation[i] = permutation[j];
                permutation[j] = value;
            }

            for (int i = 0; i < 258; i++)
            {
                permutation[256 + i] = permutation[i];
                gradientX[256 + i] = gradientX[i];
                gradientY[256 + i] = gradientY[i];
            }
        }

        public float Sample(float x, float z)
        {
            x = x * xFrequency + xOffset;
            z = z * zFrequency + zOffset;
            float sum = 0;
            float frequency = 1;
            float weight = 1;
            for (int i = 0; i < octaves; i++)
            {
                float value = Noise(x * frequency, z * frequency);
                switch (combination)
                {
                    case 2:
                        value = 1 - Math.Abs(value);
                        break;
                    case 3:
                        value = Math.Abs(value);
                        break;
                    case 4:
                        value = 1 - Math.Max(0, Math.Min(1, value));
                        break;
                    case 5:
                        value = Math.Max(0, Math.Min(1, value));
                        break;
                }

                sum += value * weight;
                frequency *= frequencyMultiplier;
                weight *= amplitude;
            }

            double result = combination < 2 ? (sum * normalization + 1.0) * 0.5 : sum * normalization;
            if (bias)
                result = Math.Pow(result, Math.Log(biasValue) / Math.Log(0.5));
            if (gain)
            {
                if (result < 0.001)
                    return 0;
                if (result > 0.999)
                    return 1;
                double exponent = Math.Log(1 - gainValue) / Math.Log(0.5);
                result = result < 0.5 ? Math.Pow(result * 2, exponent) * 0.5 : 1 - Math.Pow((1 - result) * 2, exponent) * 0.5;
            }

            return (float)result;
        }

        float Noise(double x, double z)
        {
            x += 4096;
            z += 4096;
            int ix = (int)Math.Floor(x);
            int iz = (int)Math.Floor(z);
            double rx = x - ix;
            double rz = z - iz;
            int a = permutation[ix & 255];
            int b = permutation[(ix + 1) & 255];
            double sx = rx * rx * (3 - 2 * rx);
            double sz = rz * rz * (3 - 2 * rz);
            double low = Lerp(sx, Dot(permutation[a + (iz & 255)], rx, rz), Dot(permutation[b + (iz & 255)], rx - 1, rz));
            double high = Lerp(sx, Dot(permutation[a + ((iz + 1) & 255)], rx, rz - 1), Dot(permutation[b + ((iz + 1) & 255)], rx - 1, rz - 1));
            return (float)Lerp(sz, low, high);
        }

        double Dot(int i, double x, double z) => x * gradientX[i] + z * gradientY[i];
        static double Lerp(double t, double a, double b) => a + t * (b - a);
        sealed class NativeRandom
        {
            int seed;
            int value;
            readonly int[] table = new int[322];
            public NativeRandom(int initialSeed)
            {
                seed = unchecked(-initialSeed);
            }

            void Advance()
            {
                seed = unchecked(16807 * seed - int.MaxValue * (seed / 127773));
                if (seed < 0)
                    seed += int.MaxValue;
            }

            public int Next()
            {
                if (seed <= 0 || value == 0)
                {
                    seed = Math.Max(unchecked(-seed), 1);
                    for (int i = 329; i >= 0; i--)
                    {
                        Advance();
                        if (i < 322)
                            table[i] = seed;
                    }

                    value = table[0];
                }

                Advance();
                int index = (int)(value / 6669205.0);
                value = table[index];
                table[index] = seed;
                return value;
            }
        }
    }
}
