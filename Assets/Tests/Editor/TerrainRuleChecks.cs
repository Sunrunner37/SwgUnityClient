using Swg.Client.Core.Assets.Formats.Sht;
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Formats.Tga;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Core.Characters.Materials;

namespace Swg.Client.Tests.Editor
{
    public sealed class TerrainRuleChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }

        static TerrainFloraSelection Selection(TerrainSample sample, string kind) => kind switch
        {
            "AFSC" => sample.StaticCollidable,
            "AFSN" => sample.StaticNonCollidable,
            "AFDN" => sample.DynamicNear,
            _ => sample.DynamicFar
        };
        [TestCase("AFSC")]
        [TestCase("AFSN")]
        [TestCase("AFDN")]
        [TestCase("AFDF")]
        public void FloraRulesKeepIndependentMapsAndHonorSpecificAndGlobalRemoval(string kind)
        {
            var reader = new TerrainAffectorFactory(new WorldTerrain(), null);
            TerrainLayer.Affector Rule(int family, int operation, bool removeAll) => reader.Create(TrnRule.Parse(kind, null, Bytes(writer =>
            {
                writer.Write(family);
                writer.Write(operation);
                writer.Write(removeAll ? 1 : 0);
                writer.Write(1);
                writer.Write(1f);
            })));
            var sample = TerrainSample.Empty;
            Rule(7, 1, false)(10, 20, 1, ref sample);
            Assert.That(Selection(sample, kind).FamilyId, Is.EqualTo(7));
            var repeated = TerrainSample.Empty;
            Rule(7, 1, false)(10, 20, 1, ref repeated);
            Assert.That(Selection(repeated, kind).ChildChoice, Is.EqualTo(Selection(sample, kind).ChildChoice));
            Rule(8, 0, false)(10, 20, 1, ref sample);
            Assert.That(Selection(sample, kind).FamilyId, Is.EqualTo(7));
            Rule(7, 0, false)(10, 20, 1, ref sample);
            Assert.That(Selection(sample, kind).FamilyId, Is.Zero);
            Rule(7, 1, false)(10, 20, 1, ref sample);
            Rule(0, 0, true)(10, 20, .1f, ref sample);
            Assert.That(Selection(sample, kind).FamilyId, Is.Zero);
        }

        [Test]
        public void ShaderReplacementHonorsSourceFamilyAndNativeFeatherClamp()
        {
            var reader = new TerrainAffectorFactory(new WorldTerrain(), null);
            var rule = reader.Create(TrnRule.Parse("ASRP", null, Bytes(writer =>
            {
                writer.Write(5);
                writer.Write(8);
                writer.Write(1);
                writer.Write(.7f);
            })));
            var sample = TerrainSample.Empty;
            sample.ShaderFamily = 4;
            rule(0, 0, 1, ref sample);
            Assert.That(sample.ShaderFamily, Is.EqualTo(4));
            sample.ShaderFamily = 5;
            rule(0, 0, .6f, ref sample);
            Assert.That(sample.ShaderFamily, Is.EqualTo(5));
            rule(0, 0, .7f, ref sample);
            Assert.That(sample.ShaderFamily, Is.EqualTo(8));
        }

        [Test]
        public void EnvironmentSelectionUsesItsOwnFeatherClampWithoutChangingGroundMaps()
        {
            var reader = new TerrainAffectorFactory(new WorldTerrain(), null);
            var rule = reader.Create(TrnRule.Parse("AENV", null, Bytes(writer =>
            {
                writer.Write(3);
                writer.Write(1);
                writer.Write(.4f);
            })));
            var sample = TerrainSample.Empty;
            sample.Height = 12;
            sample.ShaderFamily = 9;
            rule(0, 0, .3f, ref sample);
            Assert.That(sample.EnvironmentFamily, Is.Zero);
            rule(0, 0, .4f, ref sample);
            Assert.That(sample.EnvironmentFamily, Is.EqualTo(3));
            Assert.That(sample.Height, Is.EqualTo(12));
            Assert.That(sample.ShaderFamily, Is.EqualTo(9));
        }

        [Test]
        public void RoadInterpolatesBakedHeightsAndClearsFloraOnlyInsideItsShoulder()
        {
            var version = new IffNode
            {
                Kind = "0005"
            };
            var data = new IffNode
            {
                Kind = "DATA"
            };
            version.Children.Add(data);
            var height = new IffNode
            {
                Kind = "HDTA"
            };
            var heightVersion = new IffNode
            {
                Kind = "0001"
            };
            height.Children.Add(heightVersion);
            heightVersion.Children.Add(new IffNode { Tag = "SGMT", Data = Bytes(writer =>
            {
                writer.Write(0f);
                writer.Write(3f);
                writer.Write(0f);
                writer.Write(10f);
                writer.Write(7f);
                writer.Write(0f);
            }) });
            data.Children.Add(height);
            data.Children.Add(new IffNode { Tag = "DATA", Data = Bytes(writer =>
            {
                writer.Write(2);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(10f);
                writer.Write(0f);
                writer.Write(10f);
                writer.Write(4);
                writer.Write(0);
                writer.Write(.2f);
                writer.Write(0);
                writer.Write(.2f);
            }) });
            var road = new TerrainRoad(TrnRoad.Parse(version));
            var sample = TerrainSample.Empty;
            sample.StaticCollidable = new TerrainFloraSelection(7, .5f);
            road.Apply(5, 0, 1, ref sample);
            Assert.That(sample.Height, Is.EqualTo(5));
            Assert.That(sample.ShaderFamily, Is.EqualTo(4));
            Assert.That(sample.StaticCollidable.FamilyId, Is.Zero);
            var outside = TerrainSample.Empty;
            outside.Height = 20;
            road.Apply(5, 6, 1, ref outside);
            Assert.That(outside.Height, Is.EqualTo(20));
        }

        [Test]
        public void RadialFloraReadsTheNativeVersionTwoMainTextureBinding()
        {
            var textures = new IffNode
            {
                Kind = "TXMS"
            };
            var binding = new IffNode
            {
                Kind = "TXM "
            };
            var version = new IffNode
            {
                Kind = "0002"
            };
            version.Children.Add(new IffNode { Tag = "DATA", Data = new byte[] { 78, 73, 65, 77, 0, 2, 2, 0, 2, 2, 2, 0 } });
            version.Children.Add(new IffNode { Tag = "NAME", Data = Encoding.ASCII.GetBytes("texture\\radl_grass_yellow.dds\0") });
            binding.Children.Add(version);
            textures.Children.Add(binding);
            Assert.That(ShtFile.TryGetMainTexture(new System.Collections.Generic.List<IffNode> { textures }, out var texturePath), Is.True);
            Assert.That(texturePath, Is.EqualTo("texture/radl_grass_yellow.dds"));
        }

        [Test]
        public void MissingShaderMainTextureReturnsFalseAndClearsOutput()
        {
            Assert.That(ShtFile.TryGetMainTexture(Array.Empty<IffNode>(), out var texturePath), Is.False);
            Assert.That(texturePath, Is.Null);
        }

        [Test]
        public void TgaRampsDecodeBottomOriginAndRunLengthPackets()
        {
            var image = Bytes(writer =>
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)10);
                writer.Write(new byte[9]);
                writer.Write((ushort)2);
                writer.Write((ushort)2);
                writer.Write((byte)24);
                writer.Write((byte)0);
                writer.Write((byte)129);
                writer.Write(new byte[] { 0, 0, 255 }); // Bottom row: two red pixels.
                writer.Write((byte)129);
                writer.Write(new byte[] { 255, 0, 0 }); // Top row: two blue pixels.
            });
            var decoded = TgaFile.Parse(image);
            Assert.That(decoded.Pixels.Take(4), Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
            Assert.That(decoded.Pixels.Skip(8).Take(4), Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
        }
    }
}
