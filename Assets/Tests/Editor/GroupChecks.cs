using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Assets;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Trade;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Runtime.UI.Group;
using Swg.Client.Runtime.UI.Hud;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class GroupChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        static void Ascii(BinaryWriter writer, string value) { writer.Write((ushort)value.Length); writer.Write(System.Text.Encoding.ASCII.GetBytes(value)); }
        static GroupSceneObjectUpdate Baseline()
        {
            var reader = new ByteReader(Bytes(writer =>
            {
                writer.Write(0u); writer.Write(2); writer.Write(1u);
                writer.Write(10UL); Ascii(writer, "Leader"); writer.Write(11UL); Ascii(writer, "Member");
                writer.Write(0); writer.Write(0u); Ascii(writer, ""); writer.Write((ushort)20);
                writer.Write(0u); writer.Write(10UL); writer.Write(0u);
            }));
            var update = GroupSceneObjectUpdate.ReadBaseline6(reader);
            Assert.That(reader.Remaining, Is.Zero);
            return update;
        }
        [Test]
        public void Core3RosterAndCreatureGroupLinkUseTheSharedRegistry()
        {
            var state = GameServicesForTests.CreateState(out var player);
            player.BeginScene(11);
            state.SceneObjects.Add(new CreatureSceneObject { SwgId = 11 });
            using var replication = GameServicesForTests.CreateReplication(state, player);
            var creatureUpdate = new CreatureSceneObjectUpdate();
            Assert.That(CreatureView6.ReadDeltaField(6, new ByteReader(BitConverter.GetBytes(100UL)), creatureUpdate), Is.True);
            replication.Commit(11, creatureUpdate);
            replication.Commit(100, new ReplicationLifecycleUpdate(objectType: ReplicatedObjectType.Group), Baseline());
            replication.ApplyPending();
            Assert.That(player.Group, Is.TypeOf<GroupSceneObject>());
            Assert.That(player.Group.LeaderSwgId, Is.EqualTo(new SwgId(10)));
            Assert.That(player.Group.Members.Select(member => member.Name), Is.EqualTo(new[] { "Leader", "Member" }));
            Assert.That(creatureUpdate.Changes.HasFlag(SceneObjectChange.Group), Is.True);
            Assert.That(player.Group, Is.Not.InstanceOf<SpatialSceneObject>());
            state.SceneObjects.Remove(100);
            Assert.That(player.Group, Is.Null);
        }
        [Test]
        public void RosterDeltasSupportLeaderChangesRemovalAndClearWithoutMutatingSnapshots()
        {
            var group = new GroupSceneObject();
            group.Apply(Baseline());
            var snapshot = (GroupSceneObject)group.Copy();
            var update = new GroupSceneObjectUpdate();
            var reader = new ByteReader(Bytes(writer =>
            {
                writer.Write(2); writer.Write(2u);
                writer.Write((byte)2); writer.Write((ushort)0); writer.Write(11UL); Ascii(writer, "Member");
                writer.Write((byte)2); writer.Write((ushort)1); writer.Write(10UL); Ascii(writer, "Leader");
            }));
            Assert.That(GroupSceneObjectUpdate.ReadDeltaField(1, reader, update), Is.True);
            Assert.That(reader.Remaining, Is.Zero);
            group.Apply(update);
            Assert.That(group.LeaderSwgId, Is.EqualTo(new SwgId(11)));
            Assert.That(snapshot.LeaderSwgId, Is.EqualTo(new SwgId(10)));
            update = new GroupSceneObjectUpdate();
            GroupSceneObjectUpdate.ReadDeltaField(1, new ByteReader(Bytes(writer => { writer.Write(1); writer.Write(3u); writer.Write((byte)0); writer.Write((ushort)1); })), update);
            group.Apply(update);
            Assert.That(group.Contains(10), Is.False);
            update = new GroupSceneObjectUpdate();
            GroupSceneObjectUpdate.ReadDeltaField(1, new ByteReader(Bytes(writer => { writer.Write(1); writer.Write(4u); writer.Write((byte)4); })), update);
            group.Apply(update);
            Assert.That(group.Members, Is.Empty);
            Assert.That(snapshot.Members.Count, Is.EqualTo(2));
        }
        [Test]
        public void GroupMemberHudShowsDistanceHamAndColorsAndHidesStaleInformation()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Group/GroupMember.prefab");
            var root = UnityEngine.Object.Instantiate(source);
            var body = new GameObject("Group member test body");
            var player = new GameObject("Group player test body");
            var cameraRoot = new GameObject("Group camera");
            try
            {
                var row = root.GetComponent<GroupMemberHud>();
                var camera = cameraRoot.AddComponent<Camera>();
                var creature = new CreatureSceneObject { Ham = new HamPool { Health = new HamElement(50, 100, 0), Action = new HamElement(80, 80, 0), Mind = new HamElement(60, 60, 0) } };
                body.transform.position = Vector3.forward * 6;
                var member = new GroupMember(11, "Member");
                row.Show(member, false, creature, body, player.transform, camera);
                Assert.That(row.memberName.text, Is.EqualTo("Member (6m)"));
                Assert.That(row.memberName.color, Is.EqualTo(row.memberColor));
                Assert.That(row.statusDisplay.ham.gameObject.activeSelf, Is.True);
                Assert.That(row.direction.gameObject.activeSelf, Is.True);
                row.Show(member, true, creature, body, player.transform, camera);
                Assert.That(row.memberName.color, Is.EqualTo(row.leaderColor));
                row.Show(member, true, null, null, player.transform, camera);
                Assert.That(row.memberName.text, Is.EqualTo("Member"));
                Assert.That(row.statusDisplay.ham.gameObject.activeSelf, Is.False);
                Assert.That(row.direction.gameObject.activeSelf, Is.False);
                Assert.That(row.worldName.gameObject.activeSelf, Is.False);
                Assert.That(row.status.gameObject.activeSelf, Is.True);
                Assert.That(row.status.text, Does.Contain("Out of range"));
                row.Show(member, false, creature, body, player.transform, camera);
                Assert.That(row.statusDisplay.ham.gameObject.activeSelf, Is.True);
                Assert.That(row.status.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(cameraRoot); }
        }
        [Test]
        public void ContextActionsUseTheMemberIdAndOnlyTheLeaderCanKick()
        {
            var registry = new SceneObjectRegistry();
            var sent = new List<CommandQueueEnqueuePacket>();
            TradeRequestPacket? trade = null;
            using var services = ManagedObjectTestScope.Use(registry, builder =>
            {
                builder.Register(context => new ClientCommands(sent.Add, packet => { }, context.Resolve<CurrentPlayer>())).SingleInstance();
                builder.Register(context => new ClientTargeting(context.Resolve<WorldState>(), packet => { }, context.Resolve<CurrentPlayer>())).SingleInstance();
                builder.RegisterInstance(new Action<TradeRequestPacket>(packet => trade = packet));
            });
            GameServices.Resolve<CurrentPlayer>().BeginScene(11);
            registry.Add(new CreatureSceneObject { SwgId = 11, GroupSwgId = 100 });
            registry.Add(new CreatureSceneObject { SwgId = 10, GroupSwgId = 100 });
            var group = new GroupSceneObject { SwgId = 100 };
            group.Apply(Baseline());
            registry.Add(group);
            var target = new GameObject("Group action target");
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Group/GroupHud.prefab"));
            try
            {
                target.AddComponent<ManagedSceneObject>().Bind(10);
                var hud = root.GetComponent<GroupHud>();
                Array.Reverse(hud.menuActions); // Serialized order must not determine behavior.
                typeof(GroupHud).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                hud.OpenMenu(10, Vector2.zero);
                Assert.That(hud.menuOverlay.activeSelf, Is.True);
                Assert.That(hud.menuActions.Single(binding => binding.action == GroupHud.MemberAction.Kick).button.interactable, Is.False);
                Assert.That(hud.menuActions.Single(binding => binding.action == GroupHud.MemberAction.ImageDesign).button.interactable, Is.False);
                hud.Activate(GroupHud.MemberAction.Kick);
                Assert.That(sent, Is.Empty);
                hud.OpenMenu(10, Vector2.zero);
                hud.menuActions.Single(binding => binding.action == GroupHud.MemberAction.Trade).button.onClick.Invoke();
                Assert.That(trade.Value.TargetSwgId, Is.EqualTo(new SwgId(10)));
                hud.OpenMenu(10, Vector2.zero);
                hud.menuActions.Single(binding => binding.action == GroupHud.MemberAction.Duel).button.onClick.Invoke();
                Assert.That(sent.Single().CommandCrc, Is.EqualTo(ObjectTemplateNames.TemplateCrc("duel")));
                Assert.That(sent.Single().TargetSwgId, Is.EqualTo(10UL));
                group.Apply(new GroupSceneObjectUpdate { Members = members => { members.Reverse(); } });
                hud.OpenMenu(10, Vector2.zero);
                Assert.That(hud.menuActions.Single(binding => binding.action == GroupHud.MemberAction.Kick).button.interactable, Is.True);
                hud.Activate(GroupHud.MemberAction.Kick);
                Assert.That(sent.Last().CommandCrc, Is.EqualTo(ObjectTemplateNames.TemplateCrc("dismissgroupmember")));
                Assert.That(sent.Last().TargetSwgId, Is.EqualTo(10UL));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(target); }
        }

        [Test]
        public void TradeRequestMatchesCore3ControllerAndPayloadOffsets()
        {
            var bytes = Bytes(writer => new TradeRequestPacket(11, 12).WriteTo(writer));
            var envelope = ObjectControllerPacket.ReadFrom(new ByteReader(bytes));
            Assert.That(envelope.ControllerType, Is.EqualTo(0x115u));
            Assert.That(envelope.Flags, Is.EqualTo(0x23u));
            Assert.That(envelope.SwgId, Is.EqualTo(11UL));
            var payload = new ByteReader(envelope.Payload);
            payload.Skip(16);
            Assert.That(payload.U64(), Is.EqualTo(12UL));
            Assert.That(payload.Remaining, Is.Zero);
        }
        [Test]
        public void PlayerAndGroupShareStatusArtworkWithDistinctLayouts()
        {
            const string shared = "Assets/Prefabs/UI/Hud/CreatureStatus.prefab";
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/PlayerHud.prefab").GetComponent<PlayerHud>();
            var group = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Group/GroupMember.prefab").GetComponent<GroupMemberHud>();
            foreach (var display in new[] { player.statusDisplay, group.statusDisplay })
            {
                Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(display)), Is.EqualTo(shared));
                Assert.That(display.postureIcons.Length, Is.EqualTo(15));
                var posture = (RectTransform)display.posture.transform;
                Assert.That(posture.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(posture.sizeDelta, Is.EqualTo(new Vector2(25, display == player.statusDisplay ? 46 : 36)));
            }
            var playerHam = (RectTransform)player.statusDisplay.ham.transform;
            Assert.That(playerHam.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(playerHam.anchoredPosition, Is.EqualTo(new Vector2(-61, -7.5f)));
            Assert.That(playerHam.sizeDelta, Is.EqualTo(new Vector2(-178, -25)));
            var groupHam = (RectTransform)group.statusDisplay.ham.transform;
            Assert.That(groupHam.anchoredPosition, Is.EqualTo(new Vector2(28, -18)));
            Assert.That(groupHam.sizeDelta, Is.EqualTo(new Vector2(110, 11)));

            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(shared));
            try
            {
                var display = root.GetComponent<CreatureStatusDisplay>();
                var creature = new CreatureSceneObject { Posture = CreaturePosture.Sitting };
                display.Show(creature);
                Assert.That(display.postureIcons.Count(icon => icon.activeSelf), Is.EqualTo(1));
                Assert.That(display.postureIcons[(int)CreaturePosture.Sitting].activeSelf, Is.True);
                display.Show(null);
                Assert.That(display.posture.activeSelf, Is.False);
                Assert.That(display.ham.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void GroupLabelsRenderAndStatusArtworkFitsInsideTheRow()
        {
            var canvas = new GameObject("Group layout test", typeof(Canvas));
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Group/GroupMember.prefab");
            var root = UnityEngine.Object.Instantiate(source, canvas.transform);
            try
            {
                var row = root.GetComponent<GroupMemberHud>();
                var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/PlayerHud.prefab").GetComponent<PlayerHud>();
                var panel = (RectTransform)row.statusDisplay.transform.parent;
                Canvas.ForceUpdateCanvases();
                Assert.That(((RectTransform)root.transform).rect.width, Is.EqualTo(((RectTransform)player.panel.transform).rect.width));
                Assert.That(panel.rect.width, Is.EqualTo(((RectTransform)root.transform).rect.width));
                row.memberName.text = "Tom (6m)";
                row.memberName.ForceMeshUpdate();
                Assert.That(row.memberName.textInfo.characterCount, Is.EqualTo(row.memberName.text.Length));
                Assert.That(row.memberName.textInfo.meshInfo[0].vertexCount, Is.GreaterThan(0));
                foreach (var rect in new[] { row.memberName.rectTransform, (RectTransform)row.statusDisplay.posture.transform }
                    .Concat(row.statusDisplay.ham.currentBars.Select(bar => (RectTransform)bar.transform.parent)))
                {
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var local = panel.InverseTransformPoint(corner);
                        Assert.That(local.x, Is.InRange(panel.rect.xMin - .01f, panel.rect.xMax + .01f), rect.name);
                        Assert.That(local.y, Is.InRange(panel.rect.yMin - .01f, panel.rect.yMax + .01f), rect.name);
                    }
                }
                var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Group/GroupHud.prefab").GetComponent<GroupHud>();
                Assert.That(((RectTransform)hud.members).rect.width, Is.EqualTo(panel.rect.width));
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void ReplicationHelpersCannotBeSavedIntoTheActiveScene()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool dirty = active.isDirty;
            var state = GameServicesForTests.CreateState(out var player);
            var replication = GameServicesForTests.CreateReplication(state, player);
            Assert.That(UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(replication.gameObject.scene), Is.True);
            Assert.That(replication.gameObject.hideFlags.HasFlag(HideFlags.DontSaveInEditor), Is.True);
            Assert.That(active.isDirty, Is.EqualTo(dirty));
            GameServicesForTests.DestroyHelpers();
            Assert.That(replication == null, Is.True);
            var scene = File.ReadAllText("Assets/Scenes/UITesting.unity");
            Assert.That(scene, Does.Not.Contain("m_Name: Test replication"));
            Assert.That(scene, Does.Not.Contain("m_Name: Test client effects"));
        }

        [Test]
        public void HudPrefabUsesSharedHamAndExistingExamineWindow()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab");
            var group = source.GetComponentInChildren<GroupHud>(true);
            Assert.That(group, Is.Not.Null);
            Assert.That(group.memberPrefab.statusDisplay.ham, Is.Not.Null);
            Assert.That(group.menuActions.Length, Is.EqualTo(7));
            Assert.That(group.examine, Is.Not.Null);
            Assert.That(group.UiPrefabs.Single(), Is.EqualTo(group.memberPrefab.gameObject));
        }
    }
}
