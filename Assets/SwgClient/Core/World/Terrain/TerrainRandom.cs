using System;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Native CoordinateHash and Park-Miller generator; stable across streamed tile boundaries.</summary>
    internal struct TerrainRandom
    {
        int state;
        public TerrainRandom(float serverX, float serverZ)
        {
            uint x = BitConverter.ToUInt32(BitConverter.GetBytes(serverX), 0);
            uint z = BitConverter.ToUInt32(BitConverter.GetBytes(serverZ), 0) ^ 0xa5a5a5a5;
            state = (int)(Hash(Hash(x) ^ Hash(z)) & 0x7fffffff);
            if (state == 0)
                state = 123459876;
        }

        public float Next()
        {
            int quotient = state / 127773;
            state = 16807 * (state - quotient * 127773) - 2836 * quotient;
            if (state < 0)
                state += 2147483647;
            return (state >> 8) / 8388608f;
        }

        static uint Hash(uint value)
        {
            unchecked
            {
                value += value << 12;
                value ^= value >> 22;
                value += value << 4;
                value ^= value >> 9;
                value += value << 10;
                value ^= value >> 2;
                value += value << 7;
                return value ^ (value >> 12);
            }
        }
    }
}
