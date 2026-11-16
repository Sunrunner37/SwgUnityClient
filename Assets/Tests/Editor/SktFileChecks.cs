using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Animation;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class SktFileChecks
    {
        static byte[] Payload(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        static byte[] Chunk(string tag, byte[] payload) => Payload(writer =>
        {
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(System.Net.IPAddress.HostToNetworkOrder(payload.Length));
            writer.Write(payload);
        });
        static byte[] Form(string kind, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(kind).Concat(children.SelectMany(child => child)).ToArray());

        [Test]
        public void ParsesLocalParentsAndBindTransformsWithoutCombiningOrNormalizingThem()
        {
            byte[] Floats(params float[] values) => Payload(writer => { foreach (float value in values) writer.Write(value); });
            var bytes = Form("SKTM", Form("0002",
                Chunk("NAME", Encoding.ASCII.GetBytes("root\0child\0sibling\0")),
                Chunk("PRNT", Payload(writer => { writer.Write(-1); writer.Write(0); writer.Write(0); })),
                Chunk("BPTR", Floats(1, 2, 3, -4, 5, 6, 7, 8, -9)),
                Chunk("BPRO", Floats(1, 0, 0, 0, .4f, .1f, .2f, .3f, 1, 0, 0, 0)),
                Chunk("RPRE", Floats(1, 0, 0, 0, .8f, .5f, .6f, .7f, 1, 0, 0, 0)),
                Chunk("RPST", Floats(1, 0, 0, 0, .9f, -.1f, -.2f, -.3f, 1, 0, 0, 0))));

            var skt = SktFile.Parse(bytes);
            Assert.That(skt.Joints.Select(joint => joint.Name), Is.EqualTo(new[] { "root", "child", "sibling" }));
            Assert.That(skt.Joints.Select(joint => joint.Parent), Is.EqualTo(new[] { -1, 0, 0 }));
            Assert.That(skt.Joints[0].Translation, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(skt.Joints[1].Translation, Is.EqualTo(new Vector3(-4, 5, 6)), "Translations remain local to the parent.");
            Assert.That(skt.Joints[2].Translation, Is.EqualTo(new Vector3(7, 8, -9)));
            Assert.That(skt.Joints[0].Rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(skt.Joints[1].Rotation, Is.EqualTo(new Quaternion(.1f, .2f, .3f, .4f)));
            Assert.That(skt.Joints[1].PreRotation, Is.EqualTo(new Quaternion(.5f, .6f, .7f, .8f)));
            Assert.That(skt.Joints[1].PostRotation, Is.EqualTo(new Quaternion(-.1f, -.2f, -.3f, .9f)));
        }
    }
}
