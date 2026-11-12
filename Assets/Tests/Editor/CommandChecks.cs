using System.IO;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Tests.Editor
{
    public sealed class CommandChecks
    {
        static void Ascii(BinaryWriter w, string text)
        {
            var b = Encoding.UTF8.GetBytes(text);
            w.Write((ushort)b.Length);
            w.Write(b);
        }

        [Test]
        public void PlayerAbilitiesReadCore3BaselineAndApplyListDeltas()
        {
            var list = new List<string>();
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write(2);
                    w.Write(8u);
                    Ascii(w, "attack");
                    Ascii(w, "flourish+1");
                    w.Write(new byte[88]);
                }

                PlayerView9.ReadBaseline(new ByteReader(bytes.ToArray()))(list);
            }

            Assert.That(list, Is.EqualTo(new[] { "attack", "flourish+1" }));
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write((ushort)1);
                    w.Write((ushort)0);
                    w.Write(3);
                    w.Write(11u);
                    w.Write((byte)0);
                    w.Write((ushort)0);
                    w.Write((byte)1);
                    w.Write((ushort)1);
                    Ascii(w, "burstRun");
                    w.Write((byte)2);
                    w.Write((ushort)0);
                    Ascii(w, "aim");
                }

                Assert.That(PlayerView9.TryReadDelta(new ByteReader(bytes.ToArray()), out var update), Is.True);
                update(list);
            }

            Assert.That(list, Is.EqualTo(new[] { "aim", "burstRun" }));
        }

        [Test]
        public void AbilityClearDoesNotConsumeAnIndex()
        {
            var list = new List<string>
            {
                "attack"
            };
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write((ushort)1);
                    w.Write((ushort)0);
                    w.Write(1);
                    w.Write(1u);
                    w.Write((byte)4);
                }

                Assert.That(PlayerView9.TryReadDelta(new ByteReader(bytes.ToArray()), out var update), Is.True);
                update(list);
            }

            Assert.That(list, Is.Empty);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void UnchangedAbilitiesDoNotProduceACommandUpdate(bool emptyAbilityField)
        {
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write((ushort)1);
                    w.Write((ushort)(emptyAbilityField ? 0 : 9));
                    w.Write(0u);
                    if (emptyAbilityField)
                        w.Write(1u);
                }

                Assert.That(PlayerView9.TryReadDelta(new ByteReader(bytes.ToArray()), out var update), Is.False);
                Assert.That(update, Is.Null);
            }
        }
    }
}
