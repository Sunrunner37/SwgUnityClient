using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class ClientActivityChecks
    {
        static void Flags(BinaryWriter writer, uint value)
        {
            writer.Write(4u); writer.Write(value); writer.Write(0u); writer.Write(0u); writer.Write(0u);
        }

        [TestCase(128u, true)]
        [TestCase(129u, true)]
        [TestCase(1u, false)]
        public void PlayerBaselineReadsAfkWithoutConfusingOtherFlags(uint flags, bool expected)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(1f); writer.Write((ushort)0); writer.Write(0u); writer.Write((ushort)0);
            writer.Write(0u); writer.Write(0u); writer.Write(0u);
            Flags(writer, flags); Flags(writer, 0);
            writer.Write((ushort)0);
            for (int i = 0; i < 5; i++) writer.Write(0u);
            using var player = new CurrentPlayer(new SceneObjectRegistry());
            PlayerView3.ReadBaseline(new ByteReader(stream.ToArray()))(player);
            Assert.That(player.IsAfk, Is.EqualTo(expected));
        }

        [Test]
        public void FlagsDeltaSetsAndClearsAfkWhileOtherFieldsLeaveItAlone()
        {
            using var player = new CurrentPlayer(new SceneObjectRegistry());
            void Apply(ushort field, uint flags = 0)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write((ushort)1); writer.Write(field);
                if (field == 5) Flags(writer, flags); else writer.Write(42u);
                PlayerView3.ReadDelta(new ByteReader(stream.ToArray()))(player);
            }
            Apply(5, 128); Assert.That(player.IsAfk, Is.True);
            Apply(9); Assert.That(player.IsAfk, Is.True);
            Apply(5, 0); Assert.That(player.IsAfk, Is.False);
        }

        [Test]
        public void PublicPlayerUpdatesCannotApplyAnotherPlayersAfkFlag()
        {
            var registry = new SceneObjectRegistry();
            using var player = new CurrentPlayer(registry);
            player.BeginScene(42);
            registry.Add(new SceneObject { SwgId = 42 });
            registry.Add(new SceneObject { SwgId = 99 });
            registry.Add(new SceneObject { SwgId = 43, ParentSwgId = 42 });
            registry.Add(new SceneObject { SwgId = 100, ParentSwgId = 99 });
            var root = new GameObject("Player flags replication");
            try
            {
                var replication = root.AddComponent<GameReplication>();
                replication.Initialize(new WorldState(registry), player, new CraftingProgress(), _ => { });
                replication.UpdatePlayer(100, p => p.IsAfk = true);
                replication.ApplyPending();
                Assert.That(player.IsAfk, Is.False);
                replication.UpdatePlayer(43, p => p.IsAfk = true);
                replication.ApplyPending();
                Assert.That(player.IsAfk, Is.True);
                replication.UpdatePlayer(100, p => p.IsAfk = false);
                replication.ApplyPending();
                Assert.That(player.IsAfk, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RealActivityClearsAfkOnceBeforeActionWhileBackgroundRequestsDoNot()
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var root = new GameObject("Activity check");
            try
            {
                using var player = new CurrentPlayer(new SceneObjectRegistry());
                player.BeginScene(42);
                var sent = new List<CommandQueueEnqueuePacket>();
                using var commands = new ClientCommands(sent.Add, _ => { }, player);
                var activity = root.AddComponent<ClientActivity>(); activity.Initialize(player, commands);
                player.IsAfk = true;
                commands.Enqueue("openContainer");
                activity.UpdateActivity(false);
                Assert.That(sent.Count, Is.EqualTo(1));
                activity.UpdateActivity(true);
                commands.Enqueue("attack", targetSwgId: 99);
                Assert.That(sent[1].CommandCrc, Is.EqualTo(ObjectTemplateNames.TemplateCrc("toggleawayfromkeyboard")));
                Assert.That(sent[2].CommandCrc, Is.EqualTo(ObjectTemplateNames.TemplateCrc("attack")));
                activity.UpdateActivity(true); activity.UpdateActivity(true);
                Assert.That(sent.Count, Is.EqualTo(3), "Repeated input must not toggle AFK back on before acknowledgement.");
                player.IsAfk = false; activity.UpdateActivity(true);
                Assert.That(sent.Count, Is.EqualTo(3), "Active players must never be toggled into AFK.");
                player.IsAfk = true; activity.UpdateActivity(true);
                Assert.That(sent.Count, Is.EqualTo(4), "A later AFK session can be cleared again.");
            }
            finally { Object.DestroyImmediate(root); SynchronizationContext.SetSynchronizationContext(previous); }
        }
    }
}
