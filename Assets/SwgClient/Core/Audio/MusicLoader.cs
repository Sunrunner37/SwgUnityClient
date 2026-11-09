using System;
using System.IO;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Mp3;
using Swg.Client.Core.Assets.Formats.Sound;

namespace Swg.Client.Core.Audio
{
    /// <summary>Loads an MP3 sample directly or resolves a native sound template from the virtual asset tree.</summary>
    public static class MusicLoader
    {
        public static PcmData Load(TreAssetLoader loader, string path)
        {
            if (path.EndsWith(".snd", StringComparison.OrdinalIgnoreCase))
                path = SoundTemplate.ReadSamplePath(loader.Read(path));
            if (!path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported music sample: " + path);
            return Mp3Decoder.Decode(loader.Read(path));
        }
    }
}
