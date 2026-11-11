using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Dds;

namespace Swg.Client.Core.Assets.Formats.Tga
{
    /// <summary>TGA (Truevision Targa) images store the terrain's color and day/night lighting ramps.</summary>
    /// <seealso href="https://www.loc.gov/preservation/digital/formats/fdd/fdd000180.shtml">TGA format description</seealso>
    public static class TgaFile
    {
        public static TextureData Parse(byte[] bytes)
        {
            var reader = new ByteReader(bytes);
            int idLength = reader.Byte();
            int colorMap = reader.Byte();
            int kind = reader.Byte();
            reader.Skip(9); // Palette specification and image origin.
            int width = reader.U16();
            int height = reader.U16();
            int depth = reader.Byte();
            int descriptor = reader.Byte();

            bool invalidKind = (kind != 2 && kind != 10 && kind != 3 && kind != 11);
            bool invalidDepth = (depth != 8 && depth != 24 && depth != 32);

            if (colorMap != 0 || invalidKind || invalidDepth)
                throw new InvalidDataException("Unsupported terrain TGA encoding");

            reader.Skip(idLength);

            var result = new TextureData(width, height);
            int pixel = 0;

            while (pixel < width * height)
            {
                int packet = kind >= 10 ? reader.Byte() : 0;
                int count = kind >= 10 ? (packet & 127) + 1 : 1;
                bool repeat = (packet & 128) != 0;
                byte[] color = null;

                for (int i = 0; i < count; i++)
                {
                    if (!repeat || i == 0)
                        color = reader.Take(depth / 8);

                    if (pixel >= width * height)
                        throw new InvalidDataException("TGA pixel packet exceeds image dimensions");

                    int x = pixel % width;
                    int y = pixel / width;
                    if ((descriptor & 16) != 0) x = width - 1 - x;
                    if ((descriptor & 32) == 0) y = height - 1 - y;
                    int offset = (y * width + x) * 4;
                    result.Pixels[offset] = depth == 8 ? color[0] : color[2];
                    result.Pixels[offset + 1] = depth == 8 ? color[0] : color[1];
                    result.Pixels[offset + 2] = color[0];
                    result.Pixels[offset + 3] = depth == 32 ? color[3] : (byte)255;
                    pixel++;
                }
            }

            return result;
        }
    }
}
