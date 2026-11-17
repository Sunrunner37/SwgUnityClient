using System;
using System.IO;
using System.Text;
using Swg.Client.Core.Audio;
using Swg.Client.Core.Assets.Formats.Mp3;

namespace Swg.Client.Core.Assets.Formats.Wav
{
    /// <summary>RIFF/WAVE audio contains format and sample chunks, often with cue/loop metadata between them. Decodes PCM, IEEE float and MPEG audio.</summary>
    /// <seealso href="https://www.mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html"/>
    public static class WavFile
    {
        public static PcmData Decode(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            string Tag() => Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (Tag() != "RIFF") throw new InvalidDataException("Expected RIFF audio");
            reader.ReadUInt32();
            if (Tag() != "WAVE") throw new InvalidDataException("Expected WAVE audio");
            ushort format = 0;
            ushort channels = 0;
            ushort bits = 0;
            int rate = 0;
            byte[] data = null;
            while (stream.Position + 8 <= stream.Length)
            {
                string tag = Tag();
                int length = checked((int)reader.ReadUInt32());
                long end = checked(stream.Position + length);
                if (end > stream.Length) throw new InvalidDataException("Truncated WAVE chunk");
                if (tag == "fmt ")
                {
                    format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32();
                    reader.ReadUInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
                }
                else if (tag == "data") data = reader.ReadBytes(length);
                stream.Position = end + (length & 1);
            }
            if (data == null || channels == 0 || rate <= 0) throw new InvalidDataException("Incomplete WAVE audio");
            if (format == 0x55) return Mp3Decoder.Decode(data);
            if (!((format == 1 && (bits == 8 || bits == 16 || bits == 24 || bits == 32)) || (format == 3 && bits == 32)))
                throw new InvalidDataException("Unsupported WAVE encoding " + format + "/" + bits);
            int stride = bits / 8;
            if (data.Length % (stride * channels) != 0) throw new InvalidDataException("Incomplete WAVE frame");
            var samples = new float[data.Length / stride];
            for (int i = 0; i < samples.Length; i++)
            {
                int p = i * stride;
                samples[i] = format == 3 ? BitConverter.ToSingle(data, p) : bits == 8 ? (data[p] - 128) / 128f :
                    bits == 16 ? BitConverter.ToInt16(data, p) / 32768f : bits == 24 ?
                    ((data[p] | data[p + 1] << 8 | data[p + 2] << 16) << 8 >> 8) / 8388608f : BitConverter.ToInt32(data, p) / 2147483648f;
            }
            return new PcmData { Samples = samples, Channels = channels, Rate = rate };
        }
    }
}
