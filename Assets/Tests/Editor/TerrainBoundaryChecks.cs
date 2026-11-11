using System;
using System.IO;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.World.Terrain.Boundaries;

namespace Swg.Client.Tests.Editor
{
    public sealed class TerrainBoundaryChecks
    {
        static byte[] Payload(string kind, int featherType)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            switch (kind)
            {
                case "BCIR": writer.Write(0f); writer.Write(0f); writer.Write(10f); break;
                case "BREC": writer.Write(10f); writer.Write(10f); writer.Write(-10f); writer.Write(-10f); break;
                case "BPOL":
                    writer.Write(4); writer.Write(-10f); writer.Write(-10f); writer.Write(10f); writer.Write(-10f);
                    writer.Write(10f); writer.Write(10f); writer.Write(-10f); writer.Write(10f); break;
                case "BPLN": writer.Write(2); writer.Write(-10f); writer.Write(0f); writer.Write(10f); writer.Write(0f); break;
            }
            writer.Write(featherType);
            writer.Write(kind == "BPOL" ? 5f : .5f);
            if (kind == "BPLN") writer.Write(10f);
            return stream.ToArray();
        }

        [TestCase("BCIR", typeof(CircleBoundary), 0f, 8f, .48f)]
        [TestCase("BREC", typeof(RectangleBoundary), 8f, 0f, .4f)]
        [TestCase("BPOL", typeof(PolygonBoundary), 8f, 0f, .4f)]
        [TestCase("BPLN", typeof(PolylineBoundary), 0f, 8f, .4f)]
        public void ParsesSpecificShapesAndSamplesTheirFeatheredEdges(string kind, Type expectedType, float x, float z, float edgeWeight)
        {
            for (int featherType = 0; featherType < 4; featherType++)
            {
                var boundary = TrnBoundaryReader.Read(kind, Payload(kind, featherType));
                Assert.That(boundary, Is.TypeOf(expectedType));
                Assert.That(boundary.FeatherType, Is.EqualTo(featherType));
                Assert.That(boundary.Sample(0, 0), Is.EqualTo(1));
                Assert.That(boundary.Sample(30, 30), Is.Zero);
                float expected = featherType switch
                {
                    1 => edgeWeight * edgeWeight,
                    2 => (float)Math.Sqrt(edgeWeight),
                    3 => edgeWeight * edgeWeight * (3 - 2 * edgeWeight),
                    _ => edgeWeight
                };
                Assert.That(boundary.Sample(x, z), Is.EqualTo(expected).Within(.000001f));
            }
        }
    }
}
