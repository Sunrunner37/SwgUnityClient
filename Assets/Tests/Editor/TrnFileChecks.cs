using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Tests.Editor
{
    public sealed class TrnFileChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }

        static void CString(BinaryWriter writer, string value) => writer.Write(Encoding.ASCII.GetBytes(value + "\0"));
        static byte[] Chunk(string tag, byte[] payload) => Bytes(writer =>
        {
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(new[] { (byte)(payload.Length >> 24), (byte)(payload.Length >> 16), (byte)(payload.Length >> 8), (byte)payload.Length });
            writer.Write(payload);
        });
        static byte[] Form(string tag, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(child => child)).ToArray());
        static byte[] Data(Action<BinaryWriter> write) => Chunk("DATA", Bytes(write));
        static byte[] Enabled(bool enabled) => Form("IHDR", Form("0000", Data(writer => writer.Write(enabled ? 1 : 0))));
        static byte[] Layer(bool enabled, params byte[][] children) => Form("LAYR", Form("0000", new[] { Enabled(enabled), Chunk("ADTA", Bytes(writer =>
        {
            writer.Write(1);
            writer.Write(0);
        })) }.Concat(children).ToArray()));
        static byte[] Terrain(string version, params byte[][] generator) => Form("PTAT", Form(version, Data(writer =>
        {
            CString(writer, "terrain/example.trn");
            writer.Write(16384f);
            writer.Write(32f);
            writer.Write(4);
            writer.Write(1);
            writer.Write(12f);
        }), Form("TGEN", generator)));
        [TestCase("0013")]
        [TestCase("0014")]
        public void DecodesNestedLayersAndWeightedShaderDefinitionsWithoutAssetDependencies(string version)
        {
            var shader = Chunk("SFAM", Bytes(writer =>
            {
                writer.Write(7);
                CString(writer, "grass");
                CString(writer, "surface");
                writer.Write(new byte[] { 10, 20, 30 });
                writer.Write(8f);
                writer.Write(.6f);
                writer.Write(2);
                CString(writer, "grass_a");
                writer.Write(.25f);
                CString(writer, "grass_b");
                writer.Write(.75f);
            }));
            var height = Form("AHCN", Form("0000", Enabled(true), Data(writer =>
            {
                writer.Write(1);
                writer.Write(23f);
            })));
            var disabled = Form("AHCN", Form("0000", Enabled(false))); // Disabled rule need not have an evaluable payload.
            var unknown = Form("TEST", Form("0000", Enabled(true)));
            var file = TrnFile.Parse(Terrain(version, Form("SGRP", Form("0000", shader)), Form("LYRS", Layer(true, height, disabled, unknown, Layer(false)))));
            Assert.That(file.Version, Is.EqualTo(version));
            Assert.That(file.Size, Is.EqualTo(16384));
            Assert.That(file.ChunkSize, Is.EqualTo(32));
            Assert.That(file.TilesPerChunk, Is.EqualTo(4));
            Assert.That(file.HasGlobalWater, Is.True);
            Assert.That(file.GlobalWaterHeight, Is.EqualTo(12));
            var family = file.Shaders.Single();
            Assert.That(family.Id, Is.EqualTo(7));
            Assert.That(family.Children.Select(child => child.Path), Is.EqualTo(new[] { "grass_a", "grass_b" }));
            Assert.That(family.Children.Select(child => child.Weight), Is.EqualTo(new[] { .25f, .75f }));
            var layer = file.Layers.Single();
            Assert.That(layer.Enabled, Is.True);
            Assert.That(layer.InvertBoundaries, Is.True);
            Assert.That(layer.InvertFilters, Is.False);
            Assert.That(layer.Children.Single().Enabled, Is.False);
            Assert.That(layer.UnsupportedRules, Is.EqualTo(new[] { "TEST" }));
            var affector = (TrnHeightRule)layer.Affectors.Single();
            Assert.That(affector.Operation, Is.EqualTo(1));
            Assert.That(affector.Value, Is.EqualTo(23));
        }

        [TestCase("0003")]
        [TestCase("0004")]
        public void PreservesRadialRangesAndNativeFlags(string version)
        {
            var radial = Form("RGRP", Form(version, Chunk("RFAM", Bytes(writer =>
            {
                writer.Write(9);
                CString(writer, "grass");
                writer.Write(new byte[] { 1, 2, 3 });
                writer.Write(.4f);
                writer.Write(1);
                CString(writer, "grass.sht");
                writer.Write(2f);
                writer.Write(30f);
                writer.Write(1f);
                if (version == "0004")
                    writer.Write(3f);
                writer.Write(2f);
                if (version == "0004")
                {
                    writer.Write(6f);
                    writer.Write(1);
                }

                writer.Write(1);
                writer.Write(.5f);
                writer.Write(3f);
                writer.Write(0);
                writer.Write(1);
            }))));
            var file = TrnFile.Parse(Terrain("0014", radial, Layer(true)));
            var family = file.Flora.Single();
            Assert.That(family.Id, Is.EqualTo(9));
            Assert.That(family.Radial, Is.True);
            Assert.That(family.Density, Is.EqualTo(.4f));
            var child = family.Children.Single();
            Assert.That(child.Name, Is.EqualTo("grass.sht"));
            Assert.That(child.MinimumWidth, Is.EqualTo(1));
            Assert.That(child.MaximumWidth, Is.EqualTo(version == "0004" ? 3 : 1));
            Assert.That(child.MinimumHeight, Is.EqualTo(2));
            Assert.That(child.MaximumHeight, Is.EqualTo(version == "0004" ? 6 : 2));
            Assert.That(child.MaintainAspectRatio, Is.EqualTo(version == "0004"));
            Assert.That(child.Sways, Is.True);
            Assert.That(child.Crossed, Is.True);
        }
    }
}
