using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Cef
{
    /// <summary>CEF (Client Effect) schedules temporary appearances. CPAP records contain an appearance path and emission duration.</summary>
    public sealed class CefFile
    {
        public readonly struct Appearance
        {
            public readonly string Path;
            public readonly float Duration;
            public Appearance(string path, float duration) { Path = path; Duration = duration; }
        }
        public readonly List<Appearance> Appearances = new List<Appearance>();
        public static CefFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "CLEF") ?? throw new InvalidDataException("Expected CLEF client effect.");
            var version = root.Children.Single(n => n.Tag == "FORM");
            if (version.Kind != "0001") throw new InvalidDataException("Unsupported CLEF version " + version.Kind);
            var result = new CefFile();
            foreach (var chunk in version.Children.Where(n => n.Tag == "CPAP"))
            {
                var reader = new ByteReader(chunk.Data);
                result.Appearances.Add(new Appearance(reader.CString(), reader.Float()));
                if (reader.Remaining != 0) throw new InvalidDataException("Unexpected CPAP fields.");
            }
            return result;
        }
    }
}
