using Swg.Client.Core.Characters;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Mission;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Missions;
namespace Swg.Client.Tests.Editor
{
    public sealed class MissionChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); write(writer); return stream.ToArray(); }
        static ByteReader Callback(IPacket packet, uint controller)
        {
            var reader = new ByteReader(Bytes(packet.WriteTo));
            Assert.That(reader.U16(), Is.EqualTo(5)); Assert.That(reader.U32(), Is.EqualTo(ObjectControllerPacket.Opcode));
            Assert.That(reader.U32(), Is.EqualTo(0x23)); Assert.That(reader.U32(), Is.EqualTo(controller)); Assert.That(reader.U64(), Is.EqualTo(1));
            Assert.That(reader.U32(), Is.Zero); return reader;
        }
        [Test] public void RequestsMatchCore3CallbackFieldsWithoutExtraReservedWords()
        {
            var reader = Callback(new MissionListRequestPacket(1, 2, 7), 0xf5);
            Assert.That(reader.Byte(), Is.Zero); Assert.That(reader.Byte(), Is.EqualTo(7)); Assert.That(reader.U64(), Is.EqualTo(2)); ReplicationReader.RequireEnd(reader);
            reader = Callback(new MissionAcceptPacket(1, 3, 2), 0xf9);
            Assert.That(reader.U64(), Is.EqualTo(3)); Assert.That(reader.U64(), Is.EqualTo(2)); Assert.That(reader.Byte(), Is.Zero); ReplicationReader.RequireEnd(reader);
            reader = Callback(new MissionAbortPacket(1, 3), 0x142);
            Assert.That(reader.U64(), Is.EqualTo(3)); ReplicationReader.RequireEnd(reader);
        }
        static WorldState State(out CurrentPlayer currentPlayer)
        {
            var playerRegistry = new SceneObjectRegistry();
            currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry); currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            state.SceneObjects.Add(new SpatialSceneObject { SwgId = 1 });
            state.SceneObjects.Add(new SceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/mission_bag/shared_mission_bag.iff" });
            state.SceneObjects.Add(new SceneObject { SwgId = 3, ParentSwgId = 1, TemplatePath = "object/tangible/datapad/shared_character_datapad.iff" });
            return state;
        }
        static MissionSceneObject Offer(WorldState state, ulong id)
        { var mission = new MissionSceneObject { SwgId = id, ParentSwgId = 2, Title = "Test mission", MissionTypeCrc = 0x74ef9be3 }; state.SceneObjects.Add(mission); return mission; }
        [Test] public void DownloadWaitsForEveryCurrentRefreshAndReopensWithFreshSequence()
        {
            var state = State(out var currentPlayer); var first = Offer(state, 10); var second = Offer(state, 11); byte sequence = 0;
            using var missions = new ClientMissions(state, new StringTableLocalizer(null), packet => sequence = packet.Sequence, _ => {}, _ => {}, currentPlayer);
            missions.Open(20); Assert.That(missions.Loading, Is.True);
            first.RefreshCounter = sequence; state.SceneObjects.NotifyChanged(first, SceneObjectChange.Inventory); Assert.That(missions.Loading, Is.True);
            second.RefreshCounter = sequence; state.SceneObjects.NotifyChanged(second, SceneObjectChange.Inventory); Assert.That(missions.Loading, Is.False); Assert.That(missions.Available.Count(), Is.EqualTo(2));
            missions.Close(); missions.Open(20); Assert.That(sequence, Is.EqualTo(2)); Assert.That(missions.Available, Is.Empty);
            state.SceneObjects.NotifyChanged(second, SceneObjectChange.Inventory); Assert.That(missions.Loading, Is.True);
            first.RefreshCounter = second.RefreshCounter = sequence; state.SceneObjects.NotifyChanged(first, SceneObjectChange.Inventory); Assert.That(missions.Loading, Is.False);
        }
        [Test] public void AcceptanceAndAbortUseContainmentNotUnconditionalServerAcknowledgements()
        {
            var state = State(out var currentPlayer); var offer = Offer(state, 10); int accepts = 0; int aborts = 0;
            using var missions = new ClientMissions(state, new StringTableLocalizer(null), packet => { offer.RefreshCounter = packet.Sequence; state.SceneObjects.NotifyChanged(offer, SceneObjectChange.Inventory); }, _ => accepts++, _ => aborts++, currentPlayer);
            missions.Open(20); missions.ShowDetails(10); missions.Accept(); Assert.That(accepts, Is.EqualTo(1));
            var response = MissionAcceptResponsePacket.ReadFrom(new ByteReader(Bytes(w => { w.Write((ushort)5); w.Write(ObjectControllerPacket.Opcode); w.Write(0x1bu); w.Write(0xfau); w.Write(1ul); w.Write(0u); w.Write(10ul); w.Write((byte)1); w.Write((byte)0); })));
            missions.Receive(response); Assert.That(missions.IsAccepted(offer), Is.False); Assert.That(missions.DetailsOpen, Is.True);
            missions.Accept(); state.SceneObjects.Reparent(10, 3); Assert.That(missions.IsAccepted(offer), Is.True); Assert.That(missions.DetailsOpen, Is.False);
            missions.RequestAbort(10); missions.CancelAbort(); Assert.That(aborts, Is.Zero);
            missions.RequestAbort(10); missions.ConfirmAbort(); Assert.That(aborts, Is.EqualTo(1));
            missions.Receive(default(MissionAbortResponsePacket)); Assert.That(state.SceneObjects.TryGet(10, out _), Is.True);
            state.SceneObjects.Remove(10); Assert.That(currentPlayer.AcceptedMissions, Is.Empty);
        }
        [Test] public void MissionNavigationRetainsOrangeAndServerGroupYellowAndDeduplicates()
        {
            var state = State(out var currentPlayer); var offer = Offer(state, 10); offer.Waypoint = new Waypoint(40, new Vector3(100,0,50), state.PlanetCrc, "Mission", WaypointColor.Orange, true);
            Assert.That(currentPlayer.NavigationWaypoints, Is.Empty); state.SceneObjects.Reparent(10, 3);
            Assert.That(currentPlayer.NavigationWaypoints.Single().Color, Is.EqualTo(WaypointColor.Orange));
            currentPlayer.UpdateWaypoints(list => { list[40] = offer.Waypoint.Value; list[41] = new Waypoint(41, Vector3.zero, state.PlanetCrc, "Nearest mission for group", WaypointColor.Yellow, true); });
            Assert.That(currentPlayer.NavigationWaypoints.Count(), Is.EqualTo(2)); Assert.That(currentPlayer.NavigationWaypoints.Last().Color, Is.EqualTo(WaypointColor.Yellow));
        }
        [Test] public void CurrentPlayerPublishesMissionWaypointMembershipWithoutTheMissionService()
        {
            var state = State(out var currentPlayer);
            int notifications = 0;
            currentPlayer.WaypointsChanged += () => notifications++;
            var mission = Offer(state, 10);
            var update = new MissionSceneObjectUpdate { HasWaypoint = true, Waypoint = new Waypoint(40, Vector3.zero, state.PlanetCrc, "Mission", WaypointColor.Orange, true) };
            mission.Apply(update);
            state.SceneObjects.NotifyChanged(mission, update.Changes);
            Assert.That(notifications, Is.Zero, "Waypoint payloads for offers do not affect navigation.");

            state.SceneObjects.Reparent(10, 3);
            Assert.That(notifications, Is.EqualTo(1));
            var metadata = new MissionSceneObjectUpdate { Reward = 12500, Description = "Updated description", Difficulty = 60 };
            mission.Apply(metadata);
            state.SceneObjects.NotifyChanged(mission, metadata.Changes);
            state.SceneObjects.NotifyChanged(mission, SceneObjectChange.Transform);
            Assert.That(notifications, Is.EqualTo(1), "Metadata and movement must not rebuild waypoint views.");

            state.SceneObjects.Reparent(10, 2);
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(currentPlayer.NavigationWaypoints, Is.Empty);
            state.SceneObjects.Reparent(10, 3);
            Assert.That(notifications, Is.EqualTo(3));
            state.SceneObjects.Remove(10);
            Assert.That(notifications, Is.EqualTo(4));
            Assert.That(currentPlayer.NavigationWaypoints, Is.Empty);
        }

        [Test] public void MissionWaypointPayloadsNotifyIncludingResendsAndClears()
        {
            var state = State(out var currentPlayer);
            var mission = Offer(state, 10);
            state.SceneObjects.Reparent(10, 3);
            int notifications = 0;
            currentPlayer.WaypointsChanged += () => notifications++;
            var update = new MissionSceneObjectUpdate { HasWaypoint = true, Waypoint = new Waypoint(40, Vector3.zero, state.PlanetCrc, "Mission", WaypointColor.Orange, true) };
            mission.Apply(update);
            state.SceneObjects.NotifyChanged(mission, update.Changes);
            Assert.That(notifications, Is.EqualTo(1));
            mission.Apply(update);
            state.SceneObjects.NotifyChanged(mission, update.Changes);
            Assert.That(notifications, Is.EqualTo(2), "An identical server resend may refresh without caching old values.");
            update = new MissionSceneObjectUpdate { HasWaypoint = true };
            mission.Apply(update);
            state.SceneObjects.NotifyChanged(mission, update.Changes);
            Assert.That(notifications, Is.EqualTo(3));
            Assert.That(currentPlayer.NavigationWaypoints, Is.Empty);
        }

        [Test] public void MissionUpdatesFlagWaypointFieldsWithoutFlaggingMetadata()
        {
            Assert.That(new MissionSceneObjectUpdate { Reward = 100 }.Changes & SceneObjectChange.Waypoint, Is.EqualTo(SceneObjectChange.None));
            Assert.That(new MissionSceneObjectUpdate { HasWaypoint = true }.Changes & SceneObjectChange.Waypoint, Is.EqualTo(SceneObjectChange.Waypoint), "Clearing a waypoint carries the same flag as setting one.");
            Assert.That(SceneObjectChange.All & SceneObjectChange.Waypoint, Is.EqualTo(SceneObjectChange.Waypoint));
        }

        [Test] public void LateDatapadContainmentPublishesMissionWaypoints()
        {
            var state = State(out var currentPlayer);
            state.SceneObjects.Remove(3);
            var mission = Offer(state, 10);
            mission.Waypoint = new Waypoint(40, Vector3.zero, state.PlanetCrc, "Mission", WaypointColor.Orange, true);
            state.SceneObjects.Reparent(10, 3);
            int notifications = 0;
            currentPlayer.WaypointsChanged += () => notifications++;
            state.SceneObjects.Add(new SceneObject { SwgId = 3, ParentSwgId = 1, TemplatePath = "object/tangible/datapad/shared_character_datapad.iff" });
            Assert.That(notifications, Is.GreaterThan(0));
            Assert.That(currentPlayer.NavigationWaypoints.Single().SwgId, Is.EqualTo(new SwgId(40)));
            int beforeReparent = notifications;
            state.SceneObjects.Reparent(3, 2);
            Assert.That(notifications, Is.GreaterThan(beforeReparent), "Containment can notify for the datapad and its mission children.");
            Assert.That(currentPlayer.NavigationWaypoints, Is.Empty);
        }

        [Test] public void MissionNotificationsPreserveWaypointDeduplicationAndReleaseSubscriptions()
        {
            var state = State(out var currentPlayer);
            var mission = Offer(state, 10);
            mission.Waypoint = new Waypoint(40, Vector3.zero, state.PlanetCrc, "Mission", WaypointColor.Orange, true);
            state.SceneObjects.Reparent(10, 3);
            currentPlayer.UpdateWaypoints(list => list[40] = mission.Waypoint.Value);
            var update = new MissionSceneObjectUpdate { HasWaypoint = true, Waypoint = new Waypoint(40, Vector3.one, state.PlanetCrc, "Mission", WaypointColor.Orange, true) };
            mission.Apply(update);
            state.SceneObjects.NotifyChanged(mission, update.Changes);
            Assert.That(currentPlayer.NavigationWaypoints.Count(), Is.EqualTo(1));
            Assert.That(currentPlayer.NavigationWaypoints.Single().Position, Is.EqualTo(Vector3.zero));
            currentPlayer.UpdateWaypoints(list => list.Remove(40));
            Assert.That(currentPlayer.NavigationWaypoints.Single().Position, Is.EqualTo(Vector3.one));
            int notifications = 0;
            currentPlayer.WaypointsChanged += () => notifications++;
            currentPlayer.Dispose();
            state.SceneObjects.Remove(10);
            Assert.That(notifications, Is.Zero, "Player disposal releases the registry subscriptions.");
        }

        [Test] public void MissionDeltaRetainsTypedDataAndAllowsWaypointRemoval()
        {
            var update = new MissionSceneObjectUpdate();
            var reader = new ByteReader(Bytes(w => { w.Write(1234u); w.Write(0x74ef9be3u); }));
            MissionView3.ReadDeltaField(8, reader, update); MissionView3.ReadDeltaField(14, reader, update); ReplicationReader.RequireEnd(reader);
            var mission = new MissionSceneObject { Title = "Preserved", Waypoint = new Waypoint(40, Vector3.zero, 0, "Test", WaypointColor.Orange, true) };
            mission.Apply(update); Assert.That(mission.Reward, Is.EqualTo(1234)); Assert.That(mission.Title, Is.EqualTo("Preserved"));
            mission.Apply(new MissionSceneObjectUpdate { HasWaypoint = true }); Assert.That(mission.Waypoint, Is.Null);
            Assert.That(ReplicatedSceneObjectFactory.TypeFor(ReplicatedObjectType.Mission), Is.EqualTo(typeof(MissionSceneObject)));
        }
        [Test] public void MissionBaselineParsesCore3FieldsIncludingStringIdsAndFullWaypoint()
        {
            void Ascii(BinaryWriter w, string text) { w.Write((ushort)text.Length); w.Write(System.Text.Encoding.ASCII.GetBytes(text)); }
            void Unicode(BinaryWriter w, string text) { w.Write(text.Length); w.Write(System.Text.Encoding.Unicode.GetBytes(text)); }
            void StringId(BinaryWriter w, string file, string key) { Ascii(w,file); w.Write(0u); Ascii(w,key); }
            var reader = new ByteReader(Bytes(w => {
                w.Write(1f); StringId(w,"mission","mission_object"); Unicode(w,""); w.Write(0u); w.Write(0u);
                w.Write(56u); w.Write(10f); w.Write(0f); w.Write(30f); w.Write(0ul); w.Write(123u);
                Unicode(w,"Trixie"); w.Write(11696u); w.Write(40f); w.Write(0f); w.Write(50f); w.Write(0ul); w.Write(123u);
                w.Write(456u); StringId(w,"mission/mission_destroy_neutral_easy","m1d"); StringId(w,"mission/mission_destroy_neutral_easy","m1t");
                w.Write(7u); w.Write(0x74ef9be3u); Ascii(w,"@lair_n:ronto_nest");
                w.Write(0u); w.Write(40f); w.Write(0f); w.Write(50f); w.Write(0ul); w.Write(123u); Unicode(w,"Mission"); w.Write(400ul); w.Write((byte)3); w.Write((byte)1);
            }));
            var mission = new MissionSceneObject(); mission.Apply(MissionView3.ReadBaseline(reader)); ReplicationReader.RequireEnd(reader);
            Assert.That(mission.Difficulty, Is.EqualTo(56)); Assert.That(mission.Creator, Is.EqualTo("Trixie")); Assert.That(mission.Reward, Is.EqualTo(11696));
            Assert.That(mission.StartPosition, Is.EqualTo(new Vector3(40,0,50))); Assert.That(mission.EndPosition, Is.EqualTo(new Vector3(10,0,30)));
            Assert.That(mission.Title, Is.EqualTo("@mission/mission_destroy_neutral_easy:m1t")); Assert.That(mission.RefreshCounter, Is.EqualTo(7));
            Assert.That(mission.Waypoint.Value.Position, Is.EqualTo(new Vector3(40,0,50)));
            Assert.That(mission.Waypoint.Value.SwgId, Is.EqualTo(new SwgId(400))); Assert.That(mission.Waypoint.Value.Color, Is.EqualTo(WaypointColor.Orange)); Assert.That(mission.Waypoint.Value.Active, Is.True);
        }
        [Test] public void AcceptedMissionReusesTheDatapadRadialWithDetailsAndAbort()
        {
            var state = State(out var currentPlayer); var mission = Offer(state, 10); state.SceneObjects.Reparent(10,3);
            var menu = ObjectMenuDefaults.Create(mission, (ObjectTypeId)0x803);
            Assert.That(menu.Any(entry => entry.RadialId == ObjectMenuAction.MissionDetails.Id), Is.True);
            Assert.That(menu.Any(entry => entry.RadialId == ObjectMenuAction.MissionAbort.Id), Is.True);
        }
        [Test] public void MissionHudUsesEditableWindowsAndOriginalArtBindings()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Missions/MissionWindows.prefab").GetComponent<MissionWindows>();
            Assert.That(((RectTransform)view.browser.transform).sizeDelta, Is.EqualTo(new Vector2(736,394)));
            Assert.That(((RectTransform)view.details.transform).sizeDelta, Is.EqualTo(new Vector2(795,494)));
            Assert.That(view.downloadBlocks.Length, Is.EqualTo(9)); Assert.That(view.UiPrefabs.Count(), Is.EqualTo(2));
            Assert.That(view.detailAccept.onClick.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(ObjectMenuDefaults.TryGetDefaultAction(new TangibleSceneObject(), (ObjectTypeId)0x4006, out var action), Is.True); Assert.That(action, Is.SameAs(ObjectMenuAction.MissionList));
        }
    }
}
