using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Compression;

namespace Swg.Client.Core.Networking.Soe
{
    public static class SoeCodec
    {
        public const int Limit = 1500000;
        public static ushort Crc(uint seed, byte[] bytes, int length)
        {
            uint c = uint.MaxValue;
            byte[] prefix = BitConverter.GetBytes(seed);
            for (int i = 0; i < length + 4; i++)
            {
                c ^= i < 4 ? prefix[i] : bytes[i - 4];
                for (int bit = 0; bit < 8; bit++)
                    c = (c >> 1) ^ ((c & 1) != 0 ? 0xedb88320u : 0);
            }

            return (ushort)~c;
        }

        static void Crypt(byte[] bytes, uint seed, bool decrypt)
        {
            int start = bytes[0] == 0 ? 2 : 1;
            int end = bytes.Length - 2;
            int p = start;
            uint key = seed;
            while (p + 4 <= end)
            {
                uint input = ByteReader.At(bytes, p);
                uint output = input ^ key;
                var b = BitConverter.GetBytes(output);
                Buffer.BlockCopy(b, 0, bytes, p, 4);
                key = decrypt ? input : output;
                p += 4;
            }

            for (; p < end; p++)
                bytes[p] ^= (byte)key;
        }

        public static byte[] Encode(byte[] plain, uint seed)
        {
            var b = new byte[plain.Length + 3];
            Buffer.BlockCopy(plain, 0, b, 0, plain.Length);
            Crypt(b, seed, false);
            ushort crc = Crc(seed, b, b.Length - 2);
            b[b.Length - 2] = (byte)(crc >> 8);
            b[b.Length - 1] = (byte)crc;
            return b;
        }

        public static byte[] Decode(byte[] bytes, uint seed)
        {
            int n = bytes.Length;
            if (n < 5)
                throw new InvalidDataException("Truncated UDP packet");
            ushort crc = (ushort)(bytes[n - 2] << 8 | bytes[n - 1]);
            bool status = bytes[0] == 0 && bytes[1] == 8 && n == 43 && bytes[n - 2] == 0 && bytes[n - 1] == 0;
            if (Crc(seed, bytes, n - 2) != crc && !status)
                throw new InvalidDataException("UDP checksum mismatch");
            var b = (byte[])bytes.Clone();
            Crypt(b, seed, true);
            if (b[n - 3] == 0)
            {
                Array.Resize(ref b, n - 3);
                return b;
            }

            if (b[n - 3] != 1)
                throw new InvalidDataException("Invalid compression marker");
            int offset = b[0] == 0 ? 2 : 1;
            var packed = new byte[n - offset - 3];
            Buffer.BlockCopy(b, offset, packed, 0, packed.Length);
            var data = Zlib.Inflate(packed, Limit);
            var result = new byte[data.Length + offset];
            Buffer.BlockCopy(b, 0, result, 0, offset);
            Buffer.BlockCopy(data, 0, result, offset, data.Length);
            return result;
        }

        public static byte[] Data(ushort seq, byte[] application, byte opcode = 9)
        {
            var b = new byte[application.Length + 4];
            b[1] = opcode;
            b[2] = (byte)(seq >> 8);
            b[3] = (byte)seq;
            Buffer.BlockCopy(application, 0, b, 4, application.Length);
            return b;
        }

        public static List<byte[]> SplitMessages(byte[] b)
        {
            if (b.Length >= 2 && b[0] == 0 && b[1] == 25)
            {
                var r = new ByteReader(b)
                {
                    Position = 2
                };
                var result = new List<byte[]>();
                while (r.Remaining > 0)
                {
                    int length = r.Byte();
                    if (length == 255)
                        length = r.Net16();
                    if (length < 6)
                        throw new InvalidDataException("Invalid bundled message length");
                    result.Add(r.Take(length));
                }

                return result;
            }

            if (b.Length < 6)
                throw new InvalidDataException("Truncated application message");
            return new List<byte[]>
            {
                b
            };
        }
    }
}
