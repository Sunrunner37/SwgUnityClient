using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Audio;

namespace Swg.Client.Core.Assets.Formats.Mp3
{
    public static class Mp3Decoder
    {
        public static PcmData Decode(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, false))
            using (var mp3 = new NLayer.MpegFile(stream))
            {
                var result = new List<float>();
                var buffer = new float[16384];
                int n;
                while ((n = mp3.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    if (result.Count + n > 48000 * 2 * 600)
                        throw new InvalidDataException("Audio exceeds decode limit");
                    for (int i = 0; i < n; i++)
                        result.Add(buffer[i]);
                }

                if (result.Count == 0)
                    throw new InvalidDataException("MP3 decoded no samples");
                return new PcmData
                {
                    Samples = result.ToArray(),
                    Rate = mp3.SampleRate,
                    Channels = mp3.Channels
                };
            }
        }
    }
}
