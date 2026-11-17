using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Runtime.Networking;
using UnityEngine;

namespace Swg.Client.Runtime.Commands
{
    /// <summary>Combat projection of the shared command queue. Recovery comes from Core3's completion timer.</summary>
    public sealed class ClientCombatQueue : IDisposable
    {
        public readonly struct Entry
        {
            public readonly uint ActionCounter;
            public readonly ClientCommands.QueuedCommand Command;
            public readonly bool Executing;
            public readonly float Alpha;
            public readonly double EndsAt;
            public readonly float Duration;
            public Entry(uint actionCounter, ClientCommands.QueuedCommand command, bool executing, float alpha = 1, double endsAt = 0, float duration = 0)
            {
                ActionCounter = actionCounter;
                Command = command;
                Executing = executing;
                Alpha = alpha;
                EndsAt = endsAt;
                Duration = duration;
            }
        }
        readonly ClientCommands commands;
        readonly CommandCatalog catalog;
        (uint Counter, ClientCommands.QueuedCommand Command, double EndsAt, float Duration)? recovery;
        public event Action Changed;
        /// <summary>Local action counter and full authoritative recovery duration in seconds.</summary>
        public event Action<uint, float> RecoveryStarted;
        public ClientCombatQueue(ClientCommands commands, CommandCatalog catalog)
        {
            this.commands = commands;
            this.catalog = catalog;
            commands.Changed += OnChanged;
            commands.Completed += OnCompleted;
            catalog.Changed += OnChanged;
        }
        void OnChanged() => Changed?.Invoke();
        void OnCompleted(uint counter, ClientCommands.QueuedCommand command, CommandQueueRemovePacket result)
        {
            if (result.ErrorType == 0 && result.Timer > 0 && catalog.FindByCrc(command.CommandCrc)?.AddToCombatQueue == true)
            {
                recovery = (counter, command, Time.realtimeSinceStartupAsDouble + result.Timer, result.Timer);
                RecoveryStarted?.Invoke(counter, result.Timer);
            }
        }
        public bool TryGetRecovery(double now, out Entry entry)
        {
            entry = default;
            if (!recovery.HasValue || now >= recovery.Value.EndsAt)
                return false;
            var active = recovery.Value;
            entry = new Entry(active.Counter, active.Command, true, Mathf.Clamp01((float)(active.EndsAt - now) / active.Duration), active.EndsAt, active.Duration);
            return true;
        }
        public IEnumerable<Entry> GetEntries(double now)
        {
            bool executing = TryGetRecovery(now, out var active);
            if (executing)
                yield return active;
            foreach (var pair in commands.QueuedCommands
                .Where(pair => catalog.FindByCrc(pair.Value.CommandCrc)?.AddToCombatQueue == true)
                .OrderBy(pair => catalog.FindByCrc(pair.Value.CommandCrc).Priority).ThenBy(pair => pair.Value.Order))
            {
                yield return new Entry(pair.Key, pair.Value, !executing);
                executing = true;
            }
        }
        public void Clear()
        {
            foreach (var entry in GetEntries(Time.realtimeSinceStartupAsDouble).Where(entry => !entry.Executing).ToArray())
                commands.Cancel(entry.ActionCounter);
        }
        public void Peace()
        {
            Clear();
            commands.Enqueue("peace", completed: result =>
            {
                if (result.ErrorType != 0)
                    Debug.LogWarning("Peace failed (server error " + result.ErrorType + ":" + result.ErrorCode + ").");
            });
        }
        public void Dispose()
        {
            commands.Changed -= OnChanged;
            commands.Completed -= OnCompleted;
            catalog.Changed -= OnChanged;
            Changed = null;
            RecoveryStarted = null;
        }
    }
}
