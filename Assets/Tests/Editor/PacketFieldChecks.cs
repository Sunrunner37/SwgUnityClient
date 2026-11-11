using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Tests.Editor
{
    public sealed class PacketFieldChecks
    {
        [Serializable]
        public sealed class ExpectedField
        {
            public string path;
            public string value;
            public bool numeric;
        }

        [Serializable]
        public sealed class WireCase
        {
            public string packetType;
            public string name;
            public string kind;
            public uint value;
            public string hex;
            public ExpectedField[] expected;
        }

        [Serializable]
        sealed class Cases
        {
            public WireCase[] packets;
        }

        internal static WireCase[] Fixtures() => JsonUtility.FromJson<Cases>(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/Editor/Fixtures/Core3PacketFields.json"))).packets;
        static IEnumerable<TestCaseData> Fields()
        {
            int index = 0;
            foreach (var item in Fixtures())
                yield return new TestCaseData(item).SetName("ParsedFields_" + item.name + "_" + index++);
        }

        [TestCaseSource(nameof(Fields))]
        public void ParsesFieldsAndWritesTheSameWireLayout(WireCase fixture)
        {
            var type = typeof(GamePacketRouter).Assembly.GetType(fixture.packetType, true);
            var read = type.GetMethod("ReadFrom", new[] { typeof(ByteReader) });
            byte[] bytes = TestHex.Decode(fixture.hex);
            var packet = (IPacket)read.Invoke(null, new object[] { new ByteReader(bytes) });
            foreach (var expected in fixture.expected)
            {
                object field = packet;
                foreach (Match token in Regex.Matches(expected.path, @"[^.\[\]]+|\[\d+\]"))
                {
                    if (token.Value[0] == '[')
                        field = ((Array)field).GetValue(int.Parse(token.Value.Trim('[', ']')));
                    else
                    {
                        var member = field.GetType().GetField(token.Value);
                        field = member != null ? member.GetValue(field) : field.GetType().GetProperty(token.Value).GetValue(field);
                    }
                }

                if (expected.numeric && (field is float || field is double))
                    Assert.That(Convert.ToDouble(field, CultureInfo.InvariantCulture), Is.EqualTo(double.Parse(expected.value, CultureInfo.InvariantCulture)).Within(0.000001), expected.path);
                else
                    Assert.That(Convert.ToString(field, CultureInfo.InvariantCulture), Is.EqualTo(expected.value), expected.path);
            }

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            packet.WriteTo(writer);
            Assert.That(stream.ToArray(), Is.EqualTo(bytes));
            AssertRejected(read, bytes.Take(bytes.Length - 1).ToArray());
            // Existing login permissions parsing is outside this payload refactor.
            if (!fixture.packetType.Contains(".Login."))
                AssertRejected(read, bytes.Concat(new byte[] { 255 }).ToArray());
        }

        static void AssertRejected(MethodInfo read, byte[] bytes)
        {
            var error = Assert.Throws<TargetInvocationException>(() => read.Invoke(null, new object[] { new ByteReader(bytes) }));
            Assert.That(error.InnerException, Is.InstanceOf<InvalidDataException>().Or.InstanceOf<OverflowException>().Or.InstanceOf<ArgumentException>());
        }
    }
}
