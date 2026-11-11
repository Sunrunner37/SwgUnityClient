using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Loading;

namespace Swg.Client.Tests.Editor
{
    public static class DataTableMappingChecks
    {
        public static void Run()
        {
            var first = Table(new[] { "Count", "label", "extra" }, new[] { "i", "s", "b" }, 1, w =>
            {
                w.Write(7);
                CString(w, "Alpha");
                w.Write(1u);
            });
            var second = Table(new[] { "label", "Count" }, new[] { "s", "i" }, 1, w =>
            {
                CString(w, "Beta");
                w.Write(-3);
            });
            var row = DataTableReader.Read<BasicRow>(first).Single();
            Assert.AreEqual("Alpha", row.Name);
            Assert.AreEqual(7, row.Count);
            Assert.AreEqual(41, row.Optional, "Missing optional columns preserve model initializers.");
            Assert.AreEqual("fallback", row.Fallback);
            Assert.AreEqual(9, row.Nullable);
            Assert.AreEqual("ignored", row.Ignored);
            row = DataTableReader.Read<BasicRow>(DataTableFile.Parse(second)).Single();
            Assert.AreEqual("Beta", row.Name);
            Assert.AreEqual(-3, row.Count, "Cached metadata must not cache another table's column positions.");
            var parallel = Enumerable.Range(0, 16).Select(i => Task.Run(() => DataTableReader.Read<BasicRow>(i % 2 == 0 ? first : second).Single())).ToArray();
            Task.WaitAll(parallel);
            for (int i = 0; i < parallel.Length; i++)
                Assert.AreEqual(i % 2 == 0 ? "Alpha" : "Beta", parallel[i].Result.Name);
            var explicitOptional = Table(new[] { "label", "Count", "Optional", "Fallback", "Nullable" }, new[] { "s", "i", "i", "s", "i" }, 1, w =>
            {
                CString(w, "X");
                w.Write(2);
                w.Write(0);
                CString(w, "");
                w.Write(4);
            });
            row = DataTableReader.Read<BasicRow>(explicitOptional).Single();
            Assert.AreEqual(0, row.Optional);
            Assert.AreEqual("", row.Fallback, "Defaults apply to absent columns, not encoded empty values.");
            Assert.AreEqual(4, row.Nullable);
            var noName = Table(new[] { "Count" }, new[] { "i" }, 0, w =>
            {
            });
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<BasicRow>(noName), "Schema validation runs even for empty tables.");
            var wrongType = Table(new[] { "label", "Count" }, new[] { "s", "s" }, 0, w =>
            {
            });
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<BasicRow>(wrongType));
            var duplicate = Table(new[] { "label", "label", "Count" }, new[] { "s", "s", "i" }, 0, w =>
            {
            });
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<BasicRow>(duplicate));
            Assert.Throws<InvalidOperationException>(() => DataTableReader.Read<ReadOnlyRow>(second));
            Assert.Throws<InvalidOperationException>(() => DataTableReader.Read<InvalidConverterRow>(second));
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<InvalidDefaultRow>(second));
            var values = Table(new[] { "Value" }, new[] { "s" }, 2, w =>
            {
                CString(w, "one");
                CString(w, "two");
            });
            var converted = DataTableReader.Read<ConvertedRow>(values);
            Assert.AreEqual("1:one", converted[0].Value);
            Assert.AreEqual("2:two", converted[1].Value);
            Assert.AreEqual("1:one", DataTableReader.Read<ConvertedRow>(values)[0].Value, "Converter instances are isolated between reads.");
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<WrongSourceRow>(values));
            Assert.Throws<InvalidOperationException>(() => DataTableReader.Read<WrongTargetRow>(values));
            Assert.Throws<InvalidDataException>(() => DataTableReader.Read<ObjectResultRow>(values), "Converter output must match its declared target even when the member accepts object.");
            var fallback = DataTableReader.Read<ConvertedDefaultRow>(values);
            Assert.AreEqual("1:fallback", fallback[0].Value);
            Assert.AreEqual("2:fallback", fallback[1].Value);
            var error = Assert.Throws<InvalidDataException>(() => DataTableReader.Read<BrokenResultRow>(values));
            StringAssert.Contains("Row 1", error.Message);
            StringAssert.Contains("Value", error.Message);
            error = Assert.Throws<InvalidDataException>(() => DataTableReader.Read<ThrowingRow>(values));
            Assert.IsInstanceOf<FormatException>(error.InnerException);
            var card = Table(new[] { "Title", "Picture", "StringId", "extra" }, new[] { "s", "s", "s", "i" }, 2, w =>
            {
                CString(w, "Title");
                CString(w, "loading/corellia/example");
                CString(w, "@loading/corellia:example");
                w.Write(0);
                CString(w, "Blank");
                CString(w, "");
                CString(w, "");
                w.Write(0);
            });
            var cards = DataTableReader.Read<LoadingCard>(card);
            Assert.AreEqual("Title", cards[0].Title);
            Assert.AreEqual("texture/loading/corellia/example.dds", cards[0].PicturePath);
            Assert.AreEqual("@loading/corellia:example", cards[0].Description, "STF resolution belongs to the catalog.");
            Assert.AreEqual("", cards[1].PicturePath, "Blank native rows must remain blank for catalog filtering.");
            var path = new LoadingTexturePathConverter();
            Assert.AreEqual("texture/loading/x.DDS", path.Convert("texture\\loading\\x.DDS"));
        }

        public sealed class BasicRow
        {
            [DataColumn("label")]
            public string Name { get; set; }

            public int Count;
            [DataColumn(Optional = true)]
            public int Optional = 41;
            [DataColumn(Optional = true, DefaultValue = "fallback")]
            public string Fallback { get; set; }

            [DataColumn(Optional = true, DefaultValue = 9)]
            public int? Nullable { get; set; }

            [DataColumnIgnore]
            public string Ignored { get; set; } = "ignored";
            public string Computed => Name + Count;
        }

        public sealed class ReadOnlyRow
        {
            [DataColumn("label")]
            public string Value => "";
        }

        public sealed class InvalidConverterRow
        {
            [DataColumn("label", Converter = typeof(string))]
            public string Value { get; set; }
        }

        public sealed class InvalidDefaultRow
        {
            [DataColumn("absent", Optional = true, DefaultValue = "bad")]
            public int Value { get; set; }
        }

        public sealed class ConvertedRow
        {
            [DataColumn(Converter = typeof(SequenceConverter))]
            public string Value { get; set; }
        }

        public sealed class WrongSourceRow
        {
            [DataColumn(Converter = typeof(IntegerSourceConverter))]
            public string Value { get; set; }
        }

        public sealed class WrongTargetRow
        {
            [DataColumn(Converter = typeof(SequenceConverter))]
            public int Value { get; set; }
        }

        public sealed class BrokenResultRow
        {
            [DataColumn(Converter = typeof(BrokenConverter))]
            public string Value { get; set; }
        }

        public sealed class ThrowingRow
        {
            [DataColumn(Converter = typeof(ThrowingConverter))]
            public string Value { get; set; }
        }

        public sealed class ObjectResultRow
        {
            [DataColumn(Converter = typeof(BrokenConverter))]
            public object Value { get; set; }
        }

        public sealed class ConvertedDefaultRow
        {
            [DataColumn("absent", Optional = true, DefaultValue = "fallback", Converter = typeof(SequenceConverter))]
            public string Value { get; set; }
        }

        public class SequenceConverter : IDataTableValueConverter
        {
            int count;
            public virtual Type SourceType => typeof(string);
            public Type TargetType => typeof(string);

            public virtual object Convert(object value) => ++count + ":" + value;
        }

        public sealed class IntegerSourceConverter : SequenceConverter
        {
            public override Type SourceType => typeof(int);
        }

        public sealed class BrokenConverter : SequenceConverter
        {
            public override object Convert(object value) => 123;
        }

        public sealed class ThrowingConverter : SequenceConverter
        {
            public override object Convert(object value) => throw new FormatException("Invalid custom value.");
        }

        static byte[] Table(string[] names, string[] types, uint count, Action<BinaryWriter> cells)
        {
            var columns = Payload(w =>
            {
                w.Write(names.Length);
                foreach (string name in names)
                    CString(w, name);
            });
            var definitions = Payload(w =>
            {
                foreach (string type in types)
                    CString(w, type);
            });
            var rows = Payload(w =>
            {
                w.Write(count);
                cells(w);
            });
            return Form("DTII", Form("0001", Chunk("COLS", columns), Chunk("TYPE", definitions), Chunk("ROWS", rows)));
        }

        static byte[] Form(string kind, params byte[][] children) => Chunk("FORM", System.Text.Encoding.ASCII.GetBytes(kind).Concat(children.SelectMany(c => c)).ToArray());
        static byte[] Chunk(string tag, byte[] bytes) => Payload(w =>
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes(tag));
            w.Write(new[] { (byte)(bytes.Length >> 24), (byte)(bytes.Length >> 16), (byte)(bytes.Length >> 8), (byte)bytes.Length });
            w.Write(bytes);
        });
        static byte[] Payload(Action<BinaryWriter> write)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            write(writer);
            return memory.ToArray();
        }

        static void CString(BinaryWriter writer, string text)
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes(text));
            writer.Write((byte)0);
        }
    }
}
