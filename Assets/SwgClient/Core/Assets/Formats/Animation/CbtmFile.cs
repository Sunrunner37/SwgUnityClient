using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>CBTM (Combat Manager) selects native playback animations by attack CRC, hit disposition, and attacker/defender posture.</summary>
    public sealed class CbtmFile
    {
        readonly Dictionary<uint, IffNode> actions = new Dictionary<uint, IffNode>();
        readonly Dictionary<uint, string> names = new Dictionary<uint, string>();
        public IReadOnlyDictionary<uint, string> Actions => names;
        public readonly struct Playback
        {
            public readonly string Attacker;
            public readonly string Defender;
            public Playback(string attacker, string defender) { Attacker = attacker; Defender = defender; }
        }
        CbtmFile() { }
        public static CbtmFile Parse(byte[] bytes)
        {
            var file = new CbtmFile();
            var root = IffNode.Form(IffNode.Parse(bytes), "CBTM");
            foreach (var entry in root.Children.Single(n => n.Tag == "FORM").Children.Where(n => n.Kind == "ENTR"))
            {
                string name = new ByteReader(entry.Chunk("KEY ")).CString();
                uint crc = ObjectTemplateNames.TemplateCrc(name);
                file.names.Add(crc, name);
                file.actions.Add(crc, entry.Children.Single(n => n.Tag == "FORM"));
            }
            return file;
        }
        public bool TryGet(uint crc, byte hit, byte attackerPosture, byte defenderPosture, int variation, out Playback playback)
        {
            playback = default;
            if (!actions.TryGetValue(crc, out var node)) return false;
            while (node.Kind != "SNGL")
            {
                var entries = node.Children.Where(n => n.Kind == "ENTR").ToArray();
                IffNode selected;
                if (node.Kind == "RAND")
                {
                    int total = entries.Sum(n => (int)n.Chunk("PROB")[0]);
                    int roll = (int)((uint)variation % (uint)Math.Max(1, total));
                    selected = entries.Last();
                    foreach (var entry in entries)
                    {
                        roll -= entry.Chunk("PROB")[0];
                        if (roll < 0) { selected = entry; break; }
                    }
                }
                else
                {
                    string tag = node.Kind == "DDSP" ? "DISP" : "POST";
                    int value = node.Kind == "DDSP" ? hit : node.Kind == "AEPS" ? attackerPosture : defenderPosture;
                    selected = entries.FirstOrDefault(n => BitConverter.ToInt16(n.Chunk(tag), 0) == value)
                        ?? entries.FirstOrDefault(n => BitConverter.ToInt16(n.Chunk(tag), 0) == -1);
                    if (selected == null) return false;
                }
                node = selected.Children.Single(n => n.Tag == "FORM");
            }
            string attacker = "";
            string defender = "";
            foreach (var variable in node.Children.Where(n => n.Kind == "VARS").SelectMany(n => n.Children).Where(n => n.Tag == "STRN"))
            {
                var reader = new ByteReader(variable.Data);
                uint tag = reader.U32();
                string value = reader.CString();
                if (tag == 0x41414354) attacker = value; // AACT
                else if (tag == 0x44484143 || tag == 0x444d4143 || tag == 0x44414354) defender = value; // DHAC / DMAC / DACT
            }
            playback = new Playback(attacker, defender);
            return true;
        }
    }
}
