using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Networking.Game.Packets.Commands;

namespace Swg.Client.Runtime.Networking
{
    public sealed class ClientCommands : IDisposable
    {
        readonly Action<CommandQueueEnqueuePacket> send;
        readonly Action<CommandQueueDequeuePacket> dequeue;
        readonly CurrentPlayer currentPlayer;
        readonly SynchronizationContext context;
        public readonly struct QueuedCommand
        {
            public readonly uint CommandCrc;
            public readonly SwgId TargetSwgId;
            public readonly string Arguments;
            public readonly long Order;
            internal readonly Action<CommandQueueRemovePacket> Completed;

            internal QueuedCommand(uint commandCrc, Action<CommandQueueRemovePacket> completed = null, SwgId targetSwgId = default, string arguments = "", long order = 0)
            {
                CommandCrc = commandCrc;
                Completed = completed;
                TargetSwgId = targetSwgId;
                Arguments = arguments;
                Order = order;
            }
        }

        readonly Dictionary<uint, QueuedCommand> queued = new Dictionary<uint, QueuedCommand>();
        public IReadOnlyDictionary<uint, QueuedCommand> QueuedCommands => queued;
        public event Action Changed;
        public event Action<uint, QueuedCommand, CommandQueueRemovePacket> Completed;
        uint sequence;
        long order;
        bool disposed;
        public ClientCommands(Action<CommandQueueEnqueuePacket> send, Action<CommandQueueDequeuePacket> dequeue, CurrentPlayer currentPlayer)
        {
            this.send = send;
            this.dequeue = dequeue;
            this.currentPlayer = currentPlayer;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create ClientCommands on Unity's main thread.");
        }

        public uint Enqueue(string command, string arguments = "", SwgId targetSwgId = default, Action<CommandQueueRemovePacket> completed = null)
        {
            uint id = ++sequence;
            uint crc = ObjectTemplateNames.TemplateCrc(command.ToLowerInvariant());
            queued.Add(id, new QueuedCommand(crc, completed, targetSwgId, arguments, ++order));
            send(new CommandQueueEnqueuePacket(currentPlayer.SwgId.Value, id, crc, arguments, targetSwgId.Value));
            Changed?.Invoke();
            return id;
        }

        public void Receive(CommandQueueAddPacket packet) => context.Post(_ =>
        {
            if (disposed)
                return;
            bool known = queued.TryGetValue(packet.ActionCounter, out var existing);
            var target = existing.TargetSwgId;
            if (!known && currentPlayer.Creature is CreatureSceneObject player)
                target = player.TargetSwgId;
            queued[packet.ActionCounter] = new QueuedCommand(packet.ActionCrc, existing.Completed, target, existing.Arguments ?? "", known ? existing.Order : ++order);
            Changed?.Invoke();
        }, null);

        public void Receive(CommandQueueRemovePacket packet) => context.Post(_ =>
        {
            if (disposed)
                return;
            if (queued.TryGetValue(packet.ActionCounter, out var command))
            {
                queued.Remove(packet.ActionCounter);
                Completed?.Invoke(packet.ActionCounter, command, packet);
                command.Completed?.Invoke(packet);
                Changed?.Invoke();
            }
        }, null);

        public void Cancel(uint actionCounter)
        {
            if (!queued.TryGetValue(actionCounter, out var command))
                return;
            dequeue(new CommandQueueDequeuePacket(currentPlayer.SwgId.Value, actionCounter, command.CommandCrc));
            queued.Remove(actionCounter);
            Changed?.Invoke();
        }

        public void Dispose()
        {
            disposed = true;
            queued.Clear();
            Changed = null;
            Completed = null;
        }
    }
}
