using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Networking;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class CombatQueueChecks
    {
        sealed class Owner : SynchronizationContext
        {
            readonly Queue<Action> pending = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object state) => pending.Enqueue(() => callback(state));
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
        }
        sealed class QueueSetup : IDisposable
        {
            public readonly Owner Context = new Owner();
            public readonly WorldState State = new WorldState(new SceneObjectRegistry());
            public readonly List<CommandQueueEnqueuePacket> Sent = new List<CommandQueueEnqueuePacket>();
            public readonly List<CommandQueueDequeuePacket> Cancelled = new List<CommandQueueDequeuePacket>();
            public readonly CommandCatalog Catalog;
            public readonly ClientCommands Commands;
            public readonly ClientCombatQueue Combat;
            public QueueSetup()
            {
                State.BeginScene(42, Vector3.zero, "terrain/corellia.trn");
                State.SceneObjects.Add(new CreatureSceneObject { SwgId = 42, TargetSwgId = 99 });
                Catalog = new CommandCatalog(new[]
                {
                    new CommandCatalog.Command { Name = "attack", Label = "Attack", AddToCombatQueue = true },
                    new CommandCatalog.Command { Name = "peace", Label = "Peace", AddToCombatQueue = true, Priority = 0 },
                    new CommandCatalog.Command { Name = "spatialChatInternal", Label = "Chat" }
                }, State.Commands);
                var previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(Context);
                try { Commands = new ClientCommands(Sent.Add, Cancelled.Add, State); }
                finally { SynchronizationContext.SetSynchronizationContext(previous); }
                Combat = new ClientCombatQueue(Commands, Catalog);
            }
            public void Complete(uint counter, float timer, uint error = 0)
            {
                Commands.Receive(CommandQueueRemovePacket.ReadFrom(Packet(CommandQueueRemovePacket.ControllerType, writer =>
                {
                    writer.Write(counter); writer.Write(timer); writer.Write(error); writer.Write(0u);
                })));
                Context.Drain();
            }
            public void Dispose() { Combat.Dispose(); Commands.Dispose(); Catalog.Dispose(); }
        }
        static ObjectControllerPacket Packet(uint type, Action<BinaryWriter> payload)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x1bu); writer.Write(type); writer.Write(42ul); writer.Write(0u); payload(writer);
            return ObjectControllerPacket.ReadFrom(new ByteReader(stream.ToArray()));
        }
        [Test]
        public void ReplicatedCombatStatePreservesOtherFlagsAndNotifiesWithoutAppearanceWork()
        {
            const ulong mask = 0x100000002;
            var update = new CreatureSceneObjectUpdate();
            Assert.That(CreatureView3.ReadDeltaField(16, new ByteReader(BitConverter.GetBytes(mask)), update), Is.True);
            var creature = new CreatureSceneObject();
            creature.Apply(update);
            Assert.That(creature.IsInCombat, Is.True);
            Assert.That((ulong)creature.States, Is.EqualTo(mask));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.State));
            update.States = (ulong)CreatureState.Peace;
            creature.Apply(update);
            Assert.That(creature.IsInCombat, Is.False);
        }
        [TestCase(.5f)]
        [TestCase(5f)]
        public void CompletedActionFadesForExactlyTheServerRecoveryTimer(float timer)
        {
            using var setup = new QueueSetup();
            int callbacks = 0;
            uint first = setup.Commands.Enqueue("attack", targetSwgId: 99, completed: _ => callbacks++);
            uint second = setup.Commands.Enqueue("attack", targetSwgId: 99);
            setup.Commands.Enqueue("spatialChatInternal", "hello");
            setup.Complete(first, timer);
            double recoveryStart = Time.realtimeSinceStartupAsDouble;
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(setup.Commands.QueuedCommands.ContainsKey(first), Is.False);
            double halfwayTime = recoveryStart + timer * .5;
            Assert.That(setup.Combat.TryGetRecovery(halfwayTime, out var recovery), Is.True);
            Assert.That(recovery.ActionCounter, Is.EqualTo(first));
            Assert.That(recovery.Alpha, Is.EqualTo(.5f).Within(.001f));
            var halfway = setup.Combat.GetEntries(halfwayTime).ToArray();
            Assert.That(halfway.Length, Is.EqualTo(2), "Chat commands do not belong in the combat window");
            Assert.That(halfway[0].ActionCounter, Is.EqualTo(first));
            Assert.That(halfway[0].Executing, Is.True);
            Assert.That(halfway[0].Alpha, Is.EqualTo(.5f).Within(.001f));
            Assert.That(halfway[1].Executing, Is.False);
            Assert.That(setup.Combat.TryGetRecovery(recoveryStart + timer, out _), Is.False);
            var expired = setup.Combat.GetEntries(recoveryStart + timer + .01).ToArray();
            Assert.That(expired.Single().ActionCounter, Is.EqualTo(second));
            Assert.That(expired[0].Executing, Is.True);
        }
        [Test]
        public void ClearCancelsWaitingActionsButKeepsExecutingRecoveryAndNonCombatCommands()
        {
            using var setup = new QueueSetup();
            uint active = setup.Commands.Enqueue("attack");
            uint next = setup.Commands.Enqueue("attack");
            uint last = setup.Commands.Enqueue("attack");
            uint chat = setup.Commands.Enqueue("spatialChatInternal", "hello");
            setup.Complete(active, 10);
            double recoveryStart = Time.realtimeSinceStartupAsDouble;
            setup.Combat.Clear();
            Assert.That(setup.Cancelled.Select(packet => packet.ActionCounter), Is.EquivalentTo(new[] { next, last }));
            Assert.That(setup.Commands.QueuedCommands.Keys, Is.EquivalentTo(new[] { chat }));
            Assert.That(setup.Combat.GetEntries(recoveryStart + 9).Single().ActionCounter, Is.EqualTo(active));
        }
        [Test]
        public void ClearBeforeCompletionProtectsTheCurrentActionAndPeaceQueuesOnlyOnce()
        {
            using var setup = new QueueSetup();
            uint active = setup.Commands.Enqueue("attack");
            uint waiting = setup.Commands.Enqueue("attack");
            setup.Combat.Peace();
            Assert.That(setup.Cancelled.Single().ActionCounter, Is.EqualTo(waiting));
            Assert.That(setup.Commands.QueuedCommands.ContainsKey(active), Is.True);
            Assert.That(setup.Sent.Count(packet => packet.CommandCrc == ObjectTemplateNames.TemplateCrc("peace")), Is.EqualTo(1));
        }
        [Test]
        public void ServerAddedActionsPreserveCallbacksTargetsAndClientSequence()
        {
            using var setup = new QueueSetup();
            int callbacks = 0;
            uint first = setup.Commands.Enqueue("attack", "arguments", 99, _ => callbacks++);
            void Add(uint counter) => setup.Commands.Receive(CommandQueueAddPacket.ReadFrom(Packet(CommandQueueAddPacket.ControllerType, writer =>
            {
                writer.Write(counter); writer.Write(ObjectTemplateNames.TemplateCrc("attack"));
            })));
            Add(first); Add(0x40000020); setup.Context.Drain();
            Assert.That(setup.Commands.QueuedCommands[first].TargetSwgId.Value, Is.EqualTo(99));
            Assert.That(setup.Commands.QueuedCommands[first].Arguments, Is.EqualTo("arguments"));
            Assert.That(setup.Commands.QueuedCommands[0x40000020].TargetSwgId.Value, Is.EqualTo(99));
            Assert.That(setup.Commands.Enqueue("attack"), Is.EqualTo(first + 1));
            Assert.That(setup.Sent.Count, Is.EqualTo(2), "Server adds must not enqueue packets");
            setup.Complete(first, 0);
            Assert.That(callbacks, Is.EqualTo(1));
        }
        [Test]
        public void FailedCommandsDoNotEnterRecovery()
        {
            using var setup = new QueueSetup();
            uint counter = setup.Commands.Enqueue("attack");
            setup.Complete(counter, 10, 1);
            Assert.That(setup.Combat.GetEntries(Time.realtimeSinceStartupAsDouble), Is.Empty);
        }
        [Test]
        public void CancellationMatchesCore3CallbackFields()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            new CommandQueueDequeuePacket(42, 7, 123).WriteTo(writer);
            var packet = ObjectControllerPacket.ReadFrom(new ByteReader(stream.ToArray()));
            Assert.That(packet.Flags, Is.EqualTo(0x23));
            Assert.That(packet.SwgId, Is.EqualTo(42));
            Assert.That(packet.ControllerType, Is.EqualTo(CommandQueueDequeuePacket.ControllerType));
            var payload = new ByteReader(packet.Payload);
            Assert.That(payload.U32(), Is.EqualTo(1));
            Assert.That(payload.U32(), Is.EqualTo(7));
            Assert.That(payload.U32(), Is.EqualTo(123));
            Assert.That(payload.Remaining, Is.Zero);
        }
    }
}
