using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class LatFileChecks
    {
        [Test]
        public void SelectorValuesUseChoiceIndicesRatherThanClipNames()
        {
            byte[] values = Payload(writer =>
            {
                writer.Write((ushort)2);
                CString(writer, "vehicle_landspeeder");
                writer.Write((ushort)1);
                CString(writer, "space_sitting");
                writer.Write((ushort)0);
            });
            var choices = Form("ANMS", Chunk("INFO", new byte[] { 2, 0 }),
                Form("PXAT", Form("0000", Chunk("INFO", Text("appearance/animation/unrelated_a.ans")))),
                Form("PXAT", Form("0000", Chunk("INFO", Text("appearance/animation/unrelated_b.ans")))));
            var selector = Form("SSAT", Form("0000",
                Chunk("INFO", Text("rider_pose")), choices,
                Chunk("VAL ", values), Chunk("DFLT", new byte[] { 0, 0 })));
            var bytes = Form("LATT", Form("0000", Form("ANIM", Chunk("INFO", Text("loop_riding")), selector)));
            var animation = LatFile.Parse(bytes).Animations["loop_riding"];
            Assert.That(animation.Selections[("rider_pose", "vehicle_landspeeder")], Is.EqualTo(new[] { "appearance/animation/unrelated_b.ans" }));
            Assert.That(animation.Selections[("rider_pose", "space_sitting")], Is.EqualTo(new[] { "appearance/animation/unrelated_a.ans" }));
            Assert.That(animation.Clips.Length, Is.EqualTo(2));
        }

        [Test]
        [Category("NativeAssets")]
        public void NativeRiderSelectorRetainsVehicleAndChairAssociations()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var riding = LatFile.Parse(loader.Read("appearance/lat/all_m.lat")).Animations["loop_riding"];
            Assert.That(riding.Selections[("rider_pose", "vehicle_landspeeder")].Single(), Does.EndWith("all_b_mnt_ride_landspeeder.ans"));
            Assert.That(riding.Selections[("rider_pose", "vehicle_swoop_bike")].Single(), Does.EndWith("all_b_mnt_ride_swoop_bike.ans"));
            Assert.That(riding.Selections[("rider_pose", "space_sitting")].Single(), Does.EndWith("all_b_idl_sitting_chair_idle.ans"));
        }

        static byte[] Form(string kind, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(kind).Concat(children.SelectMany(child => child)).ToArray());
        static byte[] Chunk(string tag, byte[] payload) => Payload(writer =>
        {
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(new[] { (byte)(payload.Length >> 24), (byte)(payload.Length >> 16), (byte)(payload.Length >> 8), (byte)payload.Length });
            writer.Write(payload);
        });
        static byte[] Text(string value) => Payload(writer => CString(writer, value));
        static void CString(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.ASCII.GetBytes(value));
            writer.Write((byte)0);
        }
        static byte[] Payload(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
    }
}
