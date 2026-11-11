using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Tests.Editor
{
    public static class DataTableChecks
    {
        public static void Run()
        {
            string[] names =
            {
                "text",
                "number",
                "flag",
                "hex",
                "integer",
                "crc",
                "path",
                "enum",
                "packed",
                "template"
            };
            string[] types =
            {
                "s[default]",
                "f[1.5]",
                "b[0]",
                "h",
                "i",
                "c",
                "p",
                "e(one=1,two=2)",
                "z",
                "I"
            };
            var bytes = Table(names, types, 2, w =>
            {
                CString(w, "Tést");
                w.Write(1.25f);
                w.Write(2u);
                w.Write(0xfedcba98u);
                for (int i = 0; i < 6; i++)
                    w.Write(-7 - i);
                CString(w, "");
                w.Write(float.NaN);
                w.Write(0u);
                w.Write(uint.MaxValue);
                for (int i = 0; i < 6; i++)
                    w.Write(int.MinValue + i);
            });
            var table = DataTableFile.Parse(bytes);
            Assert.AreEqual("0001", table.Version);
            Assert.AreEqual(10, table.Columns.Count);
            Assert.AreEqual(2, table.Rows.Count);
            Assert.AreEqual("s[default]", table.Columns[0].TypeDefinition);
            Assert.AreEqual(typeof(string), table.Columns[0].ValueType);
            Assert.AreEqual(typeof(uint), table.Columns[3].ValueType);
            Assert.AreEqual("Tést", table.Rows[0].Get<string>("text"));
            Assert.AreEqual(1.25f, table.Rows[0].Get<float>(1));
            Assert.IsTrue(table.Rows[0].Get<bool>("flag"));
            Assert.AreEqual(0xfedcba98u, table.Rows[0].Get<uint>("hex"));
            for (int i = 4; i < 10; i++)
                Assert.AreEqual(-7 - (i - 4), table.Rows[0].Get<int>(i));
            Assert.AreEqual("", table.Rows[1].Get<string>(0), "Default annotations must not replace encoded empty cells.");
            Assert.IsTrue(float.IsNaN(table.Rows[1].Get<float>(1)), "Data-table floats preserve IEEE values; domain consumers validate their meaning.");
            Assert.IsFalse(table.Rows[1].Get<bool>(2));
            Assert.AreEqual(uint.MaxValue, table.Rows[1].Get<uint>(3));
            for (int i = 4; i < 10; i++)
                Assert.AreEqual(int.MinValue + i - 4, table.Rows[1].Get<int>(i));
            Assert.Throws<InvalidCastException>(() => table.Rows[0].Get<float>(0));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => table.Rows[0].Get<string>("missing"));
            Assert.IsEmpty(DataTableFile.Parse(Table(new[] { "value" }, new[] { "s" }, 0, w =>
            {
            })).Rows);
            var duplicate = DataTableFile.Parse(Table(new[] { "same", "same" }, new[] { "i", "i" }, 1, w =>
            {
                w.Write(1);
                w.Write(2);
            }));
            Assert.AreEqual(2, duplicate.Rows[0].Get<int>(1));
            Assert.Throws<InvalidOperationException>(() => duplicate.Rows[0].Get<int>("same"));
            for (int length = 0; length < bytes.Length; length++)
            {
                var truncated = bytes.Take(length).ToArray();
                Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(truncated));
            }

            Assert.Throws<NotSupportedException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "q" }, 0, w =>
            {
            })));
            Assert.Throws<NotSupportedException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "s" }, 0, w =>
            {
            }, "0002")));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "" }, 0, w =>
            {
            })));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, Array.Empty<string>(), 0, w =>
            {
            })));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "i" }, 0, w =>
            {
            }, extraColumn: "unexpected")));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "i" }, 0, w =>
            {
            }, extraType: "s")));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "i" }, 1, w => w.Write(new byte[3]))));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "s" }, 1, w =>
            {
                CString(w, "ok");
                w.Write((byte)0);
            })));
            Assert.Throws<InvalidDataException>(() => DataTableFile.Parse(Table(new[] { "value" }, new[] { "s" }, 3, w =>
            {
            })));
        }

        public static void OriginalAssets()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var doors = DataTableFile.Parse(loader.Read("datatables/appearance/door_style.iff"));
            Assert.AreEqual(18, doors.Columns.Count);
            Assert.AreEqual(85, doors.Rows.Count);
            Assert.AreEqual(typeof(bool), doors.Columns.Single(c => c.Name == "doorFlip2").ValueType);
            Assert.AreEqual(typeof(float), doors.Columns.Single(c => c.Name == "doorMoveX").ValueType);
            foreach (var row in doors.Rows)
            {
                row.Get<bool>("doorFlip2");
                row.Get<float>("doorMoveX");
            }

            var badges = DataTableFile.Parse(loader.Read("datatables/badge/badge_map.iff"));
            Assert.AreEqual(140, badges.Rows.Count);
            Assert.AreEqual("s[unused]", badges.Columns[1].TypeDefinition);
            foreach (var row in badges.Rows)
            {
                row.Get<int>("INDEX");
                row.Get<string>("KEY");
            }

            var points = DataTableFile.Parse(loader.Read("datatables/clientpoi/clientpoi.iff"));
            Assert.AreEqual(220, points.Rows.Count);
            foreach (var row in points.Rows)
            {
                row.Get<string>("Name");
                row.Get<float>("X");
                row.Get<float>("Y");
                row.Get<float>("Z");
            }
        }

        static byte[] Table(string[] names, string[] types, uint rowCount, Action<BinaryWriter> cells, string version = "0001", string extraColumn = null, string extraType = null)
        {
            var columns = Payload(w =>
            {
                w.Write(names.Length);
                foreach (string name in names)
                    CString(w, name);
                if (extraColumn != null)
                    CString(w, extraColumn);
            });
            var definitions = Payload(w =>
            {
                foreach (string type in types)
                    CString(w, type);
                if (extraType != null)
                    CString(w, extraType);
            });
            var rows = Payload(w =>
            {
                w.Write(rowCount);
                cells(w);
            });
            return Form("DTII", Form(version, Chunk("COLS", columns), Chunk("TYPE", definitions), Chunk("ROWS", rows)));
        }

        static byte[] Form(string kind, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(kind).Concat(children.SelectMany(c => c)).ToArray());
        static byte[] Chunk(string tag, byte[] payload) => Payload(w =>
        {
            w.Write(Encoding.ASCII.GetBytes(tag));
            w.Write(new[] { (byte)(payload.Length >> 24), (byte)(payload.Length >> 16), (byte)(payload.Length >> 8), (byte)payload.Length });
            w.Write(payload);
        });
        static byte[] Payload(Action<BinaryWriter> write)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            write(writer);
            return memory.ToArray();
        }

        static void CString(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.UTF8.GetBytes(value));
            writer.Write((byte)0);
        }
    }
}
