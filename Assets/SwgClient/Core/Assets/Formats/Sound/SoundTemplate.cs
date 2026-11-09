using System.IO;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Sound
{
    public static class SoundTemplate
    {
        public static string ReadSamplePath(byte[] b)
        {
            var r = new ByteReader(b);
            if (Encoding.ASCII.GetString(r.Take(4)) != "FORM" || r.Net32() != b.Length - 8 || Encoding.ASCII.GetString(r.Take(8)) != "SD2D0003" || r.Net32() != b.Length - 20 || r.U32() != 1)
                throw new InvalidDataException("Unsupported sound template");
            string sample = r.CString();
            return sample;
        }
    }
}
