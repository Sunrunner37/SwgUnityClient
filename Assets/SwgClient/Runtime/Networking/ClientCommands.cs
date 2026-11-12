using System;
using System.Collections.Generic;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.World;

namespace Swg.Client.Runtime.Networking
{
    public sealed class ClientCommands : IDisposable
    {
        readonly GameConnection connection;
        readonly WorldState state;
        readonly SynchronizationContext context;
        public readonly struct QueuedCommand
        {
            public readonly uint CommandCrc;
            internal readonly Action<CommandQueueRemovePacket> Completed;

            internal QueuedCommand(uint commandCrc, Action<CommandQueueRemovePacket> completed = null)
            {
                CommandCrc = commandCrc;
                Completed = completed;
            }
        }

        readonly Dictionary<uint, QueuedCommand> queued = new Dictionary<uint, QueuedCommand>();
        public IReadOnlyDictionary<uint, QueuedCommand> QueuedCommands => queued;
        uint sequence;
        bool disposed;
        public ClientCommands(GameConnection connection, WorldState state)
        {
            this.connection = connection;
            this.state = state;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create ClientCommands on Unity's main thread.");
        }

        public uint Enqueue(string command, string arguments = "", SwgId targetSwgId = default, Action<CommandQueueRemovePacket> completed = null)
        {
            uint id = ++sequence;
            uint crc = ObjectTemplateNames.TemplateCrc(command.ToLowerInvariant());
            queued.Add(id, new QueuedCommand(crc, completed));
            connection.Send(new CommandQueueEnqueuePacket(state.PlayerSwgId.Value, id, crc, arguments, targetSwgId.Value));
            return id;
        }

        public void Receive(CommandQueueAddPacket packet) => context.Post(_ =>
        {
            if (disposed)
                return;
            queued.TryGetValue(packet.ActionCounter, out var existing);
            queued[packet.ActionCounter] = new QueuedCommand(packet.ActionCrc, existing.Completed);
        }, null);

        public void Receive(CommandQueueRemovePacket packet) => context.Post(_ =>
        {
            if (disposed)
                return;
            if (queued.TryGetValue(packet.ActionCounter, out var command))
            {
                queued.Remove(packet.ActionCounter);
                command.Completed?.Invoke(packet);
            }
        }, null);

        public void Dispose()
        {
            disposed = true;
            queued.Clear();
        }
    }
}
