using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>PST (Playback Script Template) supplies variables and action threads. PPRT actions attach a particle appearance for an authored duration.</summary>
    public sealed class PstFile
    {
        public readonly struct Particle
        {
            public readonly string Path;
            public readonly string Hardpoint;
            public readonly float Duration;
            public Particle(string path, string hardpoint, float duration) { Path = path; Hardpoint = hardpoint; Duration = duration; }
        }
        readonly Dictionary<uint, string> strings = new Dictionary<uint, string>();
        readonly Dictionary<uint, float> floats = new Dictionary<uint, float>();
        readonly List<(uint Hardpoint, uint Appearance, uint Duration)> particles = new List<(uint, uint, uint)>();
        public bool HasTrails { get; private set; }
        public static PstFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "PBSC");
            var result = new PstFile();
            var version = root.Children.Single(n => n.Tag == "FORM");
            var variables = version.Children.Single(n => n.Kind == "VAR ");
            foreach (var node in variables.Children.Where(n => n.Tag == "STRN" || n.Tag == "FLOT"))
            {
                var reader = new ByteReader(node.Data);
                while (reader.Remaining > 0)
                {
                    uint tag = reader.U32();
                    if (node.Tag == "STRN") result.strings[tag] = reader.CString();
                    else result.floats[tag] = reader.Float();
                }
            }
            void Read(IffNode node)
            {
                if (node.Kind == "TSTR") result.HasTrails = true;
                if (node.Kind == "PPRT")
                {
                    var reader = new ByteReader(node.Children.Single(n => n.Tag == "FORM").Chunk("INFO"));
                    int actor = reader.U16();
                    uint hardpoint = reader.U32();
                    uint appearance = reader.U32();
                    uint duration = reader.U32();
                    if (actor == 0) result.particles.Add((hardpoint, appearance, duration));
                }
                foreach (var child in node.Children) Read(child);
            }
            Read(version);
            return result;
        }
        public Particle[] ResolveParticles(IffNode overrides)
        {
            if (particles.Count == 0) return Array.Empty<Particle>();
            var text = new Dictionary<uint, string>(strings);
            var numbers = new Dictionary<uint, float>(floats);
            foreach (var node in overrides.Children.Where(n => n.Tag == "STRN" || n.Tag == "FLOT"))
            {
                var reader = new ByteReader(node.Data);
                uint tag = reader.U32();
                if (node.Tag == "STRN") text[tag] = reader.CString(); else numbers[tag] = reader.Float();
            }
            return particles.Select(p => new Particle(text[p.Appearance], text[p.Hardpoint], numbers[p.Duration])).Where(p => !string.IsNullOrEmpty(p.Path)).ToArray();
        }
    }
}
