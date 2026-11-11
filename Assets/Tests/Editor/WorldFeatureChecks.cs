using Swg.Client.Core.Assets.Appearances;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class WorldFeatureChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }

        [TestCase("appearance/pt_buzzing_insects_large.prt", 6)]
        [TestCase("appearance/pt_flocking_moth_white.prt", 2)]
        [TestCase("appearance/pt_flocking_greyfish.prt", 1)]
        [TestCase("appearance/pt_pollen.prt", 1)]
        public void OriginalParticleAppearancesDecodeTheirEmittersAndTextures(string path, int count)
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var result = AppearanceLoader.LoadDetailed(assets, path);
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Particles.Count, Is.EqualTo(1));
            var effect = result.Particles[0];
            Assert.That(effect.Effect.Emitters.Count, Is.EqualTo(count));
            Assert.That(effect.Emitters.Select(emitter => emitter.Definition), Is.EqualTo(effect.Effect.Emitters));
            Assert.That(effect.Emitters.All(emitter => emitter.Texture != null), Is.True);
            foreach (var emitter in effect.Effect.Emitters)
            {
                Assert.That(emitter.Waves.Length, Is.EqualTo(16));
                Assert.That(emitter.ParticleWaves.Length, Is.EqualTo(3));
                Assert.That(emitter.Waves[14].Value(0), Is.GreaterThan(0));
            }
        }

        [Test]
        public void NativeDistantShipsDecodeLegacyEmitters()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var result = AppearanceLoader.LoadDetailed(assets, "object/static/particle/shared_particle_distant_ships.iff");
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Particles.Count, Is.GreaterThan(0));
            var attachments = result.Particles.SelectMany(p => p.Emitters).SelectMany(emitter => emitter.Attachments).SelectMany(a => a.Particles).ToArray();
            Assert.That(attachments.Length, Is.GreaterThan(0));
            Assert.That(attachments.SelectMany(a => a.Emitters).Any(emitter => emitter.Mesh != null && emitter.Mesh.Parts.Count > 0), Is.True);
        }

        [Test]
        public void HarvesterInheritsItsCollidableFloraRadius()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var circles = new FloraClearanceLoader().Load(assets, "object/installation/mining_ore/shared_mining_ore_harvester_style_2.iff");
            Assert.That(circles.Any(c => c.Collidable && c.Radius == 15), Is.True);
        }

        [Test]
        public void FloraClearanceMasksRemainIndependent()
        {
            var index = new FloraClearanceIndex(new[] { new FloraClearance(Vector3.zero, 2), new FloraClearance(new Vector3(20, 0, 0), 5, true) });
            Assert.That(index.Contains(Vector3.zero), Is.True);
            Assert.That(index.Contains(Vector3.zero, true), Is.False);
            Assert.That(index.Contains(new Vector3(20, 0, 0)), Is.False);
            Assert.That(index.Contains(new Vector3(20, 0, 0), true), Is.True);
        }

        [Test]
        public void ColorAndExcludeRulesHonorFeatherAndPreserveOtherMaps()
        {
            var factory = new TerrainAffectorFactory(new WorldTerrain(), null);
            var color = factory.Create(TrnRule.Parse("ACCN", null, Bytes(w =>
            {
                w.Write(0);
                w.Write(new byte[] { 20, 40, 60 });
            })));
            var sample = TerrainSample.Empty;
            sample.Height = 7;
            color(0, 0, .5f, ref sample);
            Assert.That(sample.Color, Is.EqualTo(new Color32(137, 147, 157, 255)));
            var exclude = factory.Create(TrnRule.Parse("AEXC", null, Array.Empty<byte>()));
            exclude(0, 0, 0, ref sample);
            Assert.That(sample.Excluded, Is.False);
            exclude(0, 0, .01f, ref sample);
            Assert.That(sample.Excluded, Is.True);
            Assert.That(sample.Height, Is.EqualTo(7));
        }

        [Test]
        public void HeightRampReadsOperationRangeAndImage()
        {
            var rule = (TrnHeightColorRampRule)TrnRule.Parse("ACRH", null, Bytes(w =>
            {
                w.Write(2);
                w.Write(-10f);
                w.Write(30f);
                w.Write(System.Text.Encoding.ASCII.GetBytes("terrain/ramp.tga\0"));
            }));
            Assert.That(rule.Operation, Is.EqualTo(2));
            Assert.That(rule.LowHeight, Is.EqualTo(-10));
            Assert.That(rule.HighHeight, Is.EqualTo(30));
            Assert.That(rule.ImagePath, Is.EqualTo("terrain/ramp.tga"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClientMovementMatchesCore3WireOrderWithoutAnExtraReservedWord(bool parented)
        {
            var packet = new DataTransformPacket(123, parented ? (SwgId)456 : 0, 1234, 25, new Vector3(10, 20, 30), new Quaternion(.1f, .2f, .3f, .9f), 2.5f);
            var bytes = Bytes(w => packet.WriteTo(w));
            Assert.That(bytes.Length, Is.EqualTo(parented ? 70 : 62));
            var reader = new ByteReader(bytes);
            Assert.That(reader.U16(), Is.EqualTo(5));
            Assert.That(reader.U32(), Is.EqualTo(0x80ce5e46));
            Assert.That(reader.U32(), Is.EqualTo(0x23));
            Assert.That(reader.U32(), Is.EqualTo(parented ? 0xf1 : 0x71));
            Assert.That(reader.U64(), Is.EqualTo(123));
            Assert.That(reader.U32(), Is.EqualTo(1234));
            Assert.That(reader.U32(), Is.EqualTo(25));
            if (parented)
                Assert.That(reader.U64(), Is.EqualTo(456));
            Assert.That(reader.Float(), Is.EqualTo(.1f));
            Assert.That(reader.Float(), Is.EqualTo(.2f));
            Assert.That(reader.Float(), Is.EqualTo(.3f));
            Assert.That(reader.Float(), Is.EqualTo(.9f));
            Assert.That(reader.Float(), Is.EqualTo(10));
            Assert.That(reader.Float(), Is.EqualTo(20));
            Assert.That(reader.Float(), Is.EqualTo(30));
            Assert.That(reader.Float(), Is.EqualTo(2.5f));
            var roundTrip = DataTransformPacket.ReadFrom(new ByteReader(bytes));
            Assert.That(roundTrip.ServerPosition, Is.EqualTo(packet.ServerPosition));
            Assert.That(roundTrip.ParentSwgId, Is.EqualTo(packet.ParentSwgId));
        }
    }
}
