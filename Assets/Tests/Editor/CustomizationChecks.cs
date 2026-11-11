using System;
using System.IO;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Cidm;
using Swg.Client.Core.Assets.Formats.Customization;

namespace Swg.Client.Tests.Editor
{
    public sealed class CustomizationChecks
    {
        [Test]
        public void SerializationEscapesZerosAndPreservesValues()
        {
            var packed = PackedCustomization.Decode(new byte[] { 1, 2, 1, 0, 2, 255, 2, 255, 3 });
            byte[] encoded = packed.Serialize();
            Assert.That(encoded, Is.EqualTo(new byte[] { 1, 2, 1, 255, 1, 2, 255, 2, 255, 3 }));
            Assert.That(PackedCustomization.Decode(encoded).Values, Is.EqualTo(packed.Values));
            Assert.That(packed.Encoded[3], Is.Zero, "Serialization must not mutate the source");
            Assert.That(PackedCustomization.Decode(Array.Empty<byte>()).Serialize(), Is.Empty);
        }

        [Test]
        public void CidmReadsNamesAndRejectsTruncatedOrDuplicateEntries()
        {
            // FORM CIDM / FORM 0001 / DATA: (1, "a"), (2, "b").
            byte[] fixture =
            {
                70,
                79,
                82,
                77,
                0,
                0,
                0,
                32,
                67,
                73,
                68,
                77,
                70,
                79,
                82,
                77,
                0,
                0,
                0,
                20,
                48,
                48,
                48,
                49,
                68,
                65,
                84,
                65,
                0,
                0,
                0,
                8,
                1,
                0,
                97,
                0,
                2,
                0,
                98,
                0
            };
            var ids = CidmFile.Parse(fixture);
            Assert.That(ids[1], Is.EqualTo("a"));
            Assert.That(ids[2], Is.EqualTo("b"));
            for (int length = 0; length < fixture.Length; length++)
                Assert.Throws<InvalidDataException>(() => CidmFile.Parse(new ArraySegment<byte>(fixture, 0, length).ToArray()));
            fixture[36] = 1;
            Assert.Throws<InvalidDataException>(() => CidmFile.Parse(fixture));
        }
    }
}
