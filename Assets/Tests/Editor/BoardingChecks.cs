using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using UnityEditor;
using Swg.Client.Runtime.UI.Travel;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class BoardingChecks
    {
        static SuiPageData TicketList(uint id, string title, params string[] rows)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(id);
            PacketFieldCodec.Ascii(writer, "Script.listBox");
            writer.Write((uint)(rows.Length + 1));
            void Text(string widget, string value)
            {
                writer.Write((byte)3);
                writer.Write(1u);
                PacketFieldCodec.Unicode(writer, value);
                writer.Write(2u);
                PacketFieldCodec.Ascii(writer, widget);
                PacketFieldCodec.Ascii(writer, "Text");
            }
            Text("bg.caption.lblTitle", title);
            for (int i = 0; i < rows.Length; i++) Text("List.dataList." + i, rows[i]);
            writer.Write(0UL);
            writer.Write(0f);
            writer.Write(0UL);
            return SuiPageData.ReadFrom(new ByteReader(bytes.ToArray()));
        }

        [Test]
        public async Task BoardingUsesTheServersRowOrderAndDoesNotChooseUntilTheUserResponds()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var sent = new List<SuiEventNotificationPacket>();
            using var travel = new ClientTravel(assets, null, null, _ => { }, sent.Add);
            Assert.That(travel.TryReceive(TicketList(42, "Select Destination", "Talus Imperial Outpost", "Coronet")), Is.True);
            await Task.Delay(30);
            Assert.That(travel.Tickets.Destinations, Is.EqualTo(new[] { "Talus Imperial Outpost", "Coronet" }));
            Assert.That(sent, Is.Empty);
            travel.UseTicket(1);
            Assert.That(sent.Single().PageId, Is.EqualTo(42));
            Assert.That(sent.Single().Arguments, Is.EqualTo(new[] { "1", "Select Destination" }));
            Assert.That(sent.Single().EventIndex, Is.Zero);
            Assert.That(travel.Tickets, Is.Null);
        }

        [Test]
        public async Task ServerCloseAndCancelDoNotBoardAndOtherListsAreNotClaimed()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var sent = new List<SuiEventNotificationPacket>();
            using var travel = new ClientTravel(assets, null, null, _ => { }, sent.Add);
            Assert.That(travel.TryReceive(TicketList(10, "Unrelated list", "Other")), Is.False);
            travel.TryReceive(TicketList(11, "Select Destination", "Talus Imperial Outpost"));
            await Task.Delay(30);
            Assert.That(sent, Is.Empty, "Even a one-row SUI list must not bypass the server callback.");
            travel.CloseTickets(10);
            await Task.Delay(30);
            Assert.That(travel.Tickets, Is.Not.Null);
            travel.CancelTickets();
            Assert.That(sent.Single().EventIndex, Is.EqualTo(1));
            travel.TryReceive(TicketList(12, "Select Destination", "Talus Imperial Outpost"));
            await Task.Delay(30);
            travel.CloseTickets(12);
            await Task.Delay(30);
            Assert.That(travel.Tickets, Is.Null);
            Assert.That(sent.Count, Is.EqualTo(1));
        }

        [Test]
        public void ZoneTransferSocialListRefreshDoesNotCorruptTheFollowingField()
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write((ushort)2);
            writer.Write((ushort)7);
            writer.Write(3u); // Core3 counts the replacement marker plus two strings.
            writer.Write(12u);
            writer.Write((byte)3);
            writer.Write((ushort)2);
            PacketFieldCodec.Ascii(writer, "Applejack");
            PacketFieldCodec.Ascii(writer, "Trixie");
            writer.Write((ushort)10);
            writer.Write(55u);
            var reader = new ByteReader(bytes.ToArray());
            Assert.That(PlayerView9.TryReadDelta(reader, out var update), Is.True);
            Assert.That(reader.Remaining, Is.Zero);
            Assert.That(update.Details, Is.Not.Null);
        }

        [Test]
        public void TicketPopupUsesFullCanvasAnchorsAndStretchingRowLabels()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Travel/TicketSelection.prefab");
            var view = prefab.GetComponent<TicketSelectionWindow>();
            var rect = (RectTransform)view.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            var label = view.rowPrefab.GetComponentInChildren<TMPro.TMP_Text>().rectTransform;
            Assert.That(label.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(label.anchorMax, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void NativeTransportLoadsBodyGearAndDoorOnTheirAnimatedJoints()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var appearance = SkeletalAppearanceLoader.LoadDetailed(assets.Loader, "object/mobile/shared_player_transport.iff");
            Assert.That(appearance.Parts.Sum(part => part.Vertices.Length), Is.GreaterThan(1000));
            var attached = appearance.Parts.Where(part => part.Skinning != null && part.Skinning.Weights.All(weight => weight.weight == 1)).ToArray();
            var joints = attached.SelectMany(part => part.Skinning.Weights.Select(weight => part.Skinning.Skeleton.Joints[weight.boneIndex].Name)).Distinct().ToArray();
            Assert.That(joints, Does.Contain("hold_transport"));
            Assert.That(joints, Does.Contain("hold_transport_door"));
            Assert.That(joints, Does.Contain("hold_transport_strut_a"));
            var skeleton = SkeletonData.Load(assets.Loader, "appearance/player_transport.sat");
            Assert.That(skeleton.Postures.ContainsKey(CreaturePosture.Prone), Is.True);
            Assert.That(skeleton.Transitions[(CreaturePosture.Upright, CreaturePosture.Prone)].Duration, Is.GreaterThan(1));
            Assert.That(skeleton.Transitions[(CreaturePosture.Prone, CreaturePosture.Upright)].Duration, Is.GreaterThan(1));
            var takeoff = skeleton.Transitions[(CreaturePosture.Upright, CreaturePosture.Prone)];
            Assert.That(skeleton.Joints.Any(joint => Vector3.Distance(takeoff.Sample(joint.Name, 0, false).Translation,
                takeoff.Sample(joint.Name, takeoff.Duration, false).Translation) > 20), Is.True, "The native flight clip must carry the body away from the ground.");
        }
    }
}
