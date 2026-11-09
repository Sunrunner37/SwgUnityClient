using System.IO;
using System.IO.Compression;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Compression
{
    public static class Zlib
    {
        public static byte[] Inflate(byte[] input, int limit, int? expected = null)
        {
            if (input.Length < 6 || (input[0] & 15) != 8 || ((input[0] << 8 | input[1]) % 31) != 0 || (input[1] & 32) != 0)
                throw new InvalidDataException("Invalid zlib header");
            using (var source = new MemoryStream(input, 2, input.Length - 6, false))
            using (var decoder = new DeflateStream(source, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var block = new byte[8192];
                int n;
                while ((n = decoder.Read(block, 0, block.Length)) > 0)
                {
                    if (output.Length + n > limit)
                        throw new InvalidDataException("Decompression limit exceeded");
                    output.Write(block, 0, n);
                }

                var data = output.ToArray();
                if (expected.HasValue && data.Length != expected.Value)
                    throw new InvalidDataException("Decompressed size mismatch");
                uint a = 1;
                uint b = 0;
                foreach (byte value in data)
                {
                    a = (a + value) % 65521;
                    b = (b + a) % 65521;
                }

                uint sum = (b << 16) | a;
                var r = new ByteReader(input);
                r.Position = input.Length - 4;
                if (r.Net32() != sum)
                    throw new InvalidDataException("Invalid zlib checksum");
                return data;
            }
        }
    }
}
