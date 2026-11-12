using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Handlers.Commands;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;

namespace Swg.Client.Tests.Editor
{
    public sealed class ClientCommandsChecks
    {
        sealed class OwnerContext : SynchronizationContext
        {
            readonly Queue<(SendOrPostCallback Callback, object State)> work = new Queue<(SendOrPostCallback, object)>();
            public override void Post(SendOrPostCallback callback, object state) => work.Enqueue((callback, state));
            public void Drain()
            {
                while (work.Count > 0)
                {
                    var item = work.Dequeue();
                    item.Callback(item.State);
                }
            }
        }

        static ClientCommands Create(OwnerContext owner)
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(owner);
            try { return new ClientCommands(null, new WorldState(new SceneObjectRegistry())); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        static ObjectControllerPacket Packet(uint type, uint counter, uint crc = 0)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x0bu);
            writer.Write(type);
            writer.Write(42ul);
            writer.Write(0u);
            writer.Write(counter);
            if (type == CommandQueueAddPacket.ControllerType)
                writer.Write(crc);
            else
            {
                writer.Write(0f);
                writer.Write(0u);
                writer.Write(0u);
            }
            return ObjectControllerPacket.ReadFrom(new ByteReader(bytes.ToArray()));
        }

        [Test]
        public void ServerQueueNotificationsTrackAndRemoveActionsOnOwnerContext()
        {
            var owner = new OwnerContext();
            using var commands = Create(owner);
            IObjectControllerHandler add = new CommandQueueAddControllerHandler(commands);
            IObjectControllerHandler remove = new CommandQueueRemoveControllerHandler(commands);
            add.Handle(Packet(CommandQueueAddPacket.ControllerType, 0x40000020, 123));
            add.Handle(Packet(CommandQueueAddPacket.ControllerType, 0x40000040, 456));
            Assert.That(commands.QueuedCommands, Is.Empty);
            owner.Drain();
            Assert.That(commands.QueuedCommands[0x40000020].CommandCrc, Is.EqualTo(123));
            Assert.That(commands.QueuedCommands[0x40000040].CommandCrc, Is.EqualTo(456));
            remove.Handle(Packet(CommandQueueRemovePacket.ControllerType, 0x40000020));
            owner.Drain();
            Assert.That(commands.QueuedCommands.ContainsKey(0x40000020), Is.False);
            Assert.That(commands.QueuedCommands[0x40000040].CommandCrc, Is.EqualTo(456));
            remove.Handle(Packet(CommandQueueRemovePacket.ControllerType, 0x40000040));
            owner.Drain();
            Assert.That(commands.QueuedCommands, Is.Empty);
            // No connection is supplied: receiving an add must never enqueue another packet.
        }

        [Test]
        public void ServerAddPreservesCompletionAndRemoveDeletesBeforeInvokingIt()
        {
            var owner = new OwnerContext();
            using var commands = Create(owner);
            int completions = 0;
            var queue = (IDictionary<uint, ClientCommands.QueuedCommand>)commands.QueuedCommands;
            queue.Add(3, new ClientCommands.QueuedCommand(123, packet =>
            {
                Assert.That(commands.QueuedCommands.ContainsKey(packet.ActionCounter), Is.False);
                completions++;
            }));
            commands.Receive(CommandQueueAddPacket.ReadFrom(Packet(CommandQueueAddPacket.ControllerType, 3, 456)));
            owner.Drain();
            Assert.That(commands.QueuedCommands[3].CommandCrc, Is.EqualTo(456));
            commands.Receive(CommandQueueRemovePacket.ReadFrom(Packet(CommandQueueRemovePacket.ControllerType, 3)));
            commands.Receive(CommandQueueRemovePacket.ReadFrom(Packet(CommandQueueRemovePacket.ControllerType, 3)));
            owner.Drain();
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(commands.QueuedCommands, Is.Empty);
        }

        [Test]
        public void DisposalClearsQueuedCommandsAndIgnoresQueuedNotifications()
        {
            var owner = new OwnerContext();
            var commands = Create(owner);
            commands.Receive(CommandQueueAddPacket.ReadFrom(Packet(CommandQueueAddPacket.ControllerType, 0x40000020, 123)));
            owner.Drain();
            Assert.That(commands.QueuedCommands.Count, Is.EqualTo(1));
            commands.Receive(CommandQueueAddPacket.ReadFrom(Packet(CommandQueueAddPacket.ControllerType, 0x40000040, 456)));
            commands.Dispose();
            owner.Drain();
            Assert.That(commands.QueuedCommands, Is.Empty);
        }
    }
}
