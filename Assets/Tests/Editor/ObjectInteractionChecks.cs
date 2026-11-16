using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using UnityEditor;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class ObjectInteractionChecks
    {
        sealed class MainThread : SynchronizationContext
        {
            readonly Queue<Action> pending = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object value) => pending.Enqueue(() => callback(value));
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
        }
        [Test]
        public void MenuSelectionForwardsNonVehicleActionsWithoutLocalObjectChecks()
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new MainThread());
            try
            {
                ObjectMenuSelectPacket sent = default;
                var state = GameServicesForTests.CreateState(out var currentPlayer);
                using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, packet => sent = packet, (name, arguments, target) => { }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                interactions.SelectMenuAction(123, 118); // Server-provided structure management action.
                Assert.That(sent.TargetSwgId, Is.EqualTo((SwgId)123));
                Assert.That(sent.RadialId, Is.EqualTo(118));
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
        [Test]
        public void MenuRequestMatchesCore3ControllerLayout()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            new ObjectMenuRequestPacket(100, 200, new[] { new RadialMenuEntry(1, 0, 7, 1, "@ui_radial:examine") }, 9).WriteTo(writer);
            var reader = new ByteReader(stream.ToArray());
            Assert.That(reader.U16(), Is.EqualTo(5));
            Assert.That(reader.U32(), Is.EqualTo(0x80ce5e46));
            Assert.That(reader.U32(), Is.EqualTo(0x23));
            Assert.That(reader.U32(), Is.EqualTo(0x146));
            Assert.That(reader.U64(), Is.EqualTo(100));
            Assert.That(reader.U32(), Is.Zero); // Callback consumes this as its legacy size word.
            Assert.That(reader.U64(), Is.EqualTo(200));
            Assert.That(reader.U64(), Is.EqualTo(100));
            Assert.That(reader.U32(), Is.EqualTo(1));
            var entry = RadialMenuEntry.ReadFrom(reader);
            Assert.That(entry.RadialId, Is.EqualTo(7));
            Assert.That(entry.Text, Is.EqualTo("@ui_radial:examine"));
            Assert.That(reader.Byte(), Is.EqualTo(9));
            Assert.That(reader.Remaining, Is.Zero);
            var parsed = ObjectMenuRequestPacket.ReadFrom(new ByteReader(stream.ToArray()));
            Assert.That(parsed.TargetSwgId, Is.EqualTo((SwgId)200));
        }
        [Test]
        public void RepliesAreMarshalledAndOnlyUpdateTheCurrentRequest()
        {
            var previous = SynchronizationContext.Current;
            var main = new MainThread();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
                ObjectMenuRequestPacket request = default;
                SwgId examined = default;
                using var interactions = new ClientObjectInteractions(state, packet => request = packet, swgId => examined = swgId, packet => { }, (name, arguments, target) => { }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                interactions.RequestMenu(100);
                byte oldCounter = request.Counter;
                interactions.RequestMenu(200);
                var entries = new[] { new RadialMenuEntry(1, 0, 7, 3, "Examine"), new RadialMenuEntry(2, 0, 118, 3, "Management"), new RadialMenuEntry(3, 2, 124, 3, "Status") };
                interactions.Receive(new ObjectMenuResponsePacket(0, 200, entries, oldCounter));
                main.Drain();
                Assert.That(interactions.Menu.Count, Is.EqualTo(1));
                interactions.Receive(new ObjectMenuResponsePacket(0, 200, entries, request.Counter));
                Assert.That(interactions.Menu.Count, Is.EqualTo(1));
                main.Drain();
                Assert.That(interactions.Menu.Count, Is.EqualTo(3));
                Assert.That(interactions.Menu[2].ParentIndex, Is.EqualTo(2));
                interactions.RequestExamine(200);
                Assert.That(examined, Is.EqualTo((SwgId)200));
                interactions.Receive(new AttributeListPacket(100, new[] { new AttributeValue("owner", "Wrong") }));
                interactions.Receive(new AttributeListPacket(200, new[] { new AttributeValue("owner", "Trixie") }));
                main.Drain();
                Assert.That(interactions.GetAttributes(200)[0].Value, Is.EqualTo("Trixie"));
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
        [Test]
        public void ConcurrentExamineRequestsKeepTheirOwnResultsAndNotifications()
        {
            var previous = SynchronizationContext.Current;
            var main = new MainThread();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var state = GameServicesForTests.CreateState(out var currentPlayer);
                using var interactions = new ClientObjectInteractions(state, packet => { }, swgId => { }, packet => { }, (name, arguments, target) => { }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                var notifications = new List<SwgId>();
                interactions.AttributesChanged += notifications.Add;
                interactions.RequestExamine(100);
                interactions.RequestExamine(200);
                interactions.Receive(new AttributeListPacket(200, new[] { new AttributeValue("owner", "Trixie") }));
                interactions.Receive(new AttributeListPacket(100, new[] { new AttributeValue("owner", "Misty") }));
                main.Drain();
                Assert.That(interactions.GetAttributes(100)[0].Value, Is.EqualTo("Misty"));
                Assert.That(interactions.GetAttributes(200)[0].Value, Is.EqualTo("Trixie"));
                Assert.That(notifications, Is.EqualTo(new SwgId[] { 200, 100 }));
                interactions.RequestExamine(100);
                Assert.That(interactions.GetAttributes(100)[0].Value, Is.EqualTo("Misty"), "Keep cached details visible while refreshing.");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
        [Test]
        public void DisposingGameInteractionsSuppressesAlreadyPostedReplies()
        {
            var previous = SynchronizationContext.Current;
            var main = new MainThread();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                ObjectMenuRequestPacket request = default;
                var state = GameServicesForTests.CreateState(out var currentPlayer);
                using var interactions = new ClientObjectInteractions(state, packet => request = packet, swgId => { }, packet => { }, (name, arguments, target) => { }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                interactions.RequestMenu(200);
                interactions.RequestExamine(200);
                int notifications = 0;
                interactions.MenuChanged += () => notifications++;
                interactions.AttributesChanged += swgId => notifications++;
                interactions.Receive(new ObjectMenuResponsePacket(0, 200, new[] { new RadialMenuEntry(1, 0, 7, 3, "Examine") }, request.Counter));
                interactions.Receive(new AttributeListPacket(200, new[] { new AttributeValue("owner", "Trixie") }));
                interactions.Dispose();
                main.Drain();
                Assert.That(notifications, Is.Zero);
                Assert.That(interactions.GetAttributes(200), Is.Empty);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
        [Test]
        public void HudPrefabsHaveInteractionReferencesAndPreviewShaders()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World.prefab");
            var input = root.GetComponentInChildren<Swg.Client.Runtime.World.SceneObjects.WorldTargeting>(true);
            Assert.That(input.contextMenu, Is.Not.Null);
            Assert.That(input.contextMenu.examineWindow, Is.Not.Null);
            var preview = input.contextMenu.examineWindow.preview;
            Assert.That(preview.worldShader, Is.Not.Null);
            Assert.That(preview.characterShader, Is.Not.Null);
            Assert.That(preview.previewCamera.cullingMask, Is.EqualTo(1 << 31));
            Assert.That(input.worldCamera.cullingMask & (1 << 31), Is.Zero);
        }
    }
}
