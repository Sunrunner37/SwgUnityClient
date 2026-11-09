using System;
using System.IO;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Assets.Formats.Dds
{
    /// <summary>
    /// DDS (DirectDraw Surface) stores texture pixels, including block-compressed DXT variants.
    /// This class decodes supported DDS images into top-down RGBA pixels for shared asset consumers.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide">Microsoft DDS programming guide</seealso>
    public sealed class TextureData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Pixels;
        public TextureData(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > 8192 || height > 8192)
                throw new InvalidDataException("Invalid texture size");
            Width = width;
            Height = height;
            Pixels = new byte[checked(width * height * 4)];
        }

        public static TextureData Load(TreAssetLoader loader, string path) => DecodeDds(loader.Read(path));
        public static TextureData DecodeDds(byte[] b)
        {
            if (b.Length < 128 || Encoding.ASCII.GetString(b, 0, 4) != "DDS " || ByteReader.At(b, 4) != 124 || ByteReader.At(b, 76) != 32)
                throw new InvalidDataException("Invalid DDS header");
            int width = checked((int)ByteReader.At(b, 16));
            int height = checked((int)ByteReader.At(b, 12));
            var t = new TextureData(width, height);
            uint flags = ByteReader.At(b, 80);
            if ((flags & 4) != 0)
            {
                string four = Encoding.ASCII.GetString(b, 84, 4);
                if (four != "DXT1" && four != "DXT3" && four != "DXT5")
                    throw new InvalidDataException("Unsupported DDS format " + four);
                int blockSize = four == "DXT1" ? 8 : 16;
                int bw = (width + 3) / 4;
                int bh = (height + 3) / 4;
                if (b.Length - 128 < checked(bw * bh * blockSize))
                    throw new InvalidDataException("Truncated DDS blocks");
                for (int y = 0; y < bh; y++)
                    for (int x = 0; x < bw; x++)
                        DecodeBlock(t, b, 128 + (y * bw + x) * blockSize, x * 4, y * 4, four);
                return t;
            }

            int bits = checked((int)ByteReader.At(b, 88));
            if ((flags & 64) == 0 || (bits != 16 && bits != 24 && bits != 32))
                throw new InvalidDataException("Unsupported RGB DDS");
            int bytes = bits / 8;
            int pitch = (ByteReader.At(b, 8) & 8) != 0 ? checked((int)ByteReader.At(b, 20)) : width * bytes;
            if (pitch < width * bytes || (long)pitch * height > b.Length - 128)
                throw new InvalidDataException("Truncated DDS pixels");
            uint[] masks =
            {
                ByteReader.At(b, 92),
                ByteReader.At(b, 96),
                ByteReader.At(b, 100),
                ByteReader.At(b, 104)
            };
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int p = 128 + y * pitch + x * bytes;
                    uint value = 0;
                    for (int i = 0; i < bytes; i++)
                        value |= (uint)b[p + i] << (8 * i);
                    int dst = (y * width + x) * 4;
                    for (int c = 0; c < 4; c++)
                    {
                        uint mask = masks[c];
                        if (mask == 0)
                        {
                            t.Pixels[dst + c] = (byte)(c == 3 ? 255 : 0);
                            continue;
                        }

                        int shift = 0;
                        while ((mask & 1) == 0)
                        {
                            mask >>= 1;
                            shift++;
                        }

                        t.Pixels[dst + c] = (byte)(((ulong)((value >> shift) & mask) * 255) / mask);
                    }
                }

            return t;
        }

        static byte[] Rgb565(ushort v) => new[]
        {
            (byte)(((v >> 11) & 31) * 255 / 31),
            (byte)(((v >> 5) & 63) * 255 / 63),
            (byte)((v & 31) * 255 / 31),
            (byte)255
        };
        static void DecodeBlock(TextureData dst, byte[] b, int p, int bx, int by, string four)
        {
            int color = p + (four == "DXT1" ? 0 : 8);
            ushort c0 = (ushort)(b[color] | b[color + 1] << 8);
            ushort c1 = (ushort)(b[color + 2] | b[color + 3] << 8);
            byte[][] colors =
            {
                Rgb565(c0),
                Rgb565(c1),
                new byte[4],
                new byte[4]
            };
            if (c0 > c1 || four != "DXT1")
            {
                for (int c = 0; c < 3; c++)
                {
                    colors[2][c] = (byte)((2 * colors[0][c] + colors[1][c]) / 3);
                    colors[3][c] = (byte)((colors[0][c] + 2 * colors[1][c]) / 3);
                }

                colors[2][3] = colors[3][3] = 255;
            }
            else
            {
                for (int c = 0; c < 3; c++)
                    colors[2][c] = (byte)((colors[0][c] + colors[1][c]) / 2);
                colors[2][3] = 255;
            }

            uint indices = ByteReader.At(b, color + 4);
            ulong alphaBits = 0;
            byte[] alpha = new byte[8];
            if (four == "DXT5")
            {
                alpha[0] = b[p];
                alpha[1] = b[p + 1];
                if (alpha[0] > alpha[1])
                    for (int i = 1; i <= 6; i++)
                        alpha[i + 1] = (byte)(((7 - i) * alpha[0] + i * alpha[1]) / 7);
                else
                {
                    for (int i = 1; i <= 4; i++)
                        alpha[i + 1] = (byte)(((5 - i) * alpha[0] + i * alpha[1]) / 5);
                    alpha[6] = 0;
                    alpha[7] = 255;
                }

                for (int i = 0; i < 6; i++)
                    alphaBits |= (ulong)b[p + 2 + i] << (i * 8);
            }

            for (int i = 0; i < 16; i++)
            {
                int x = bx + i % 4;
                int y = by + i / 4;
                if (x >= dst.Width || y >= dst.Height)
                    continue;
                byte[] rgba = colors[(indices >> (i * 2)) & 3];
                int d = (y * dst.Width + x) * 4;
                Buffer.BlockCopy(rgba, 0, dst.Pixels, d, 4);
                if (four == "DXT3")
                    dst.Pixels[d + 3] = (byte)(((b[p + i / 2] >> ((i % 2) * 4)) & 15) * 17);
                else if (four == "DXT5")
                    dst.Pixels[d + 3] = alpha[(alphaBits >> (i * 3)) & 7];
            }
        }

        public TextureData Crop(int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x + w > Width || y + h > Height)
                throw new InvalidDataException("Texture crop outside image");
            var result = new TextureData(w, h);
            for (int row = 0; row < h; row++)
                Buffer.BlockCopy(Pixels, ((y + row) * Width + x) * 4, result.Pixels, row * w * 4, w * 4);
            return result;
        }
    }
}
