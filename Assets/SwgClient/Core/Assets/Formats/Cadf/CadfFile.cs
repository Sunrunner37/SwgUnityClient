using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Cadf
{
    /// <summary>
    /// CADF is SWG's IFF-based character-appearance cache (.ca): FORM CADF/0000 holds
    /// DATA (template/customization), PLAN (planet), and WEAR/ITEM (equipped appearances).
    /// </summary>
    public sealed class CadfFile
    {
        public string TemplatePath { get; set; } = "";
        public byte[] Customization { get; set; } = Array.Empty<byte>();
        public string Planet { get; set; } = "";
        public IReadOnlyList<CadfWearable> Wearables { get; set; } = Array.Empty<CadfWearable>();

        public static CadfFile Parse(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            var roots = IffNode.Parse(bytes);
            if (roots.Count != 1 || roots[0].Kind != "CADF")
                throw new InvalidDataException("Not a CADF file");
            var versions = roots[0].Children.Where(node => node.Tag == "FORM").ToArray();
            if (versions.Length != 1 || versions[0].Kind != "0000")
                throw new InvalidDataException("Unsupported CADF version");
            var version = versions[0];
            var chunks = version.Children.Where(node => node.Tag == "DATA").ToArray();
            if (chunks.Length != 1)
                throw new InvalidDataException("Missing or duplicated CADF DATA");
            var reader = new ByteReader(chunks[0].Data);
            var file = new CadfFile
            {
                TemplatePath = ReadString(reader),
                Customization = ReadBytes(reader)
            };
            End(reader);
            var planets = version.Children.Where(node => node.Tag == "PLAN").ToArray();
            if (planets.Length > 1)
                throw new InvalidDataException("Duplicated CADF PLAN");
            if (planets.Length == 1)
            {
                reader = new ByteReader(planets[0].Data);
                file.Planet = ReadString(reader);
                End(reader);
            }

            var wear = version.Children.Where(node => node.Tag == "FORM" && node.Kind == "WEAR").ToArray();
            if (wear.Length > 1)
                throw new InvalidDataException("Duplicated CADF WEAR");
            var items = new List<CadfWearable>();
            if (wear.Length == 1)
                foreach (var item in wear[0].Children.Where(node => node.Tag == "ITEM"))
                {
                    reader = new ByteReader(item.Data);
                    var value = new CadfWearable
                    {
                        TemplatePath = ReadString(reader),
                        Customization = ReadBytes(reader),
                        Arrangement = reader.U32()
                    };
                    End(reader);
                    items.Add(value);
                }

            file.Wearables = items.AsReadOnly();
            return file;
        }

        public byte[] Serialize()
        {
            byte[] Record(string template, byte[] customization, uint? arrangement = null)
            {
                using var memory = new MemoryStream();
                using var writer = new BinaryWriter(memory);
                WriteString(writer, template);
                if (customization == null || customization.Contains((byte)0))
                    throw new InvalidDataException("CADF customization must be a zero-free encoded string");
                writer.Write(customization);
                writer.Write((byte)0);
                if (arrangement.HasValue)
                    writer.Write(arrangement.Value);
                return memory.ToArray();
            }

            using var planet = new MemoryStream();
            using (var writer = new BinaryWriter(planet, Encoding.ASCII, true))
                WriteString(writer, Planet);
            return Form("CADF", new[] { Form("0000", new[] { Chunk("DATA", Record(TemplatePath, Customization)), Chunk("PLAN", planet.ToArray()), Form("WEAR", Wearables.Select(item => Chunk("ITEM", Record(item.TemplatePath, item.Customization, item.Arrangement)))) }) });
        }

        static byte[] Form(string kind, IEnumerable<byte[]> chunks) => Chunk("FORM", Encoding.ASCII.GetBytes(kind).Concat(chunks.SelectMany(chunk => chunk)).ToArray());
        static byte[] Chunk(string tag, byte[] payload)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(new[] { (byte)(payload.Length >> 24), (byte)(payload.Length >> 16), (byte)(payload.Length >> 8), (byte)payload.Length });
            writer.Write(payload);
            return memory.ToArray();
        }

        static void WriteString(BinaryWriter writer, string value)
        {
            if (value == null || value.Any(c => c == 0 || c > 127))
                throw new InvalidDataException("Invalid CADF ASCII string");
            writer.Write(Encoding.ASCII.GetBytes(value));
            writer.Write((byte)0);
        }

        static string ReadString(ByteReader reader)
        {
            byte[] bytes = ReadBytes(reader);
            if (bytes.Any(value => value > 127))
                throw new InvalidDataException("Invalid CADF ASCII string");
            return Encoding.ASCII.GetString(bytes);
        }

        static byte[] ReadBytes(ByteReader reader)
        {
            int start = reader.Position;
            while (reader.Byte() != 0)
            {
            }

            var bytes = new byte[reader.Position - start - 1];
            Buffer.BlockCopy(reader.Bytes, start, bytes, 0, bytes.Length);
            return bytes;
        }

        static void End(ByteReader reader)
        {
            if (reader.Remaining != 0)
                throw new InvalidDataException("Unexpected CADF chunk fields");
        }
    }
}
