using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking.Chat;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Server-driven NPC dialogue. Responses are zero-based Core3 conversation choices, not local scripts.</summary>
    public sealed class ClientConversation : IDisposable
    {
        readonly Action<string, string, SwgId> enqueue;
        readonly SynchronizationContext context;
        readonly StringTableLocalizer localizer;
        readonly ChatText text;
        bool disposed;
        public SwgId NpcSwgId { get; private set; }
        public bool IsOpen => !NpcSwgId.IsNone;
        public string Message { get; private set; } = "";
        public IReadOnlyList<string> Options { get; private set; } = Array.Empty<string>();
        public bool Waiting { get; private set; }
        public event Action Changed;

        public ClientConversation(Action<string, string, SwgId> enqueue, StringTableLocalizer localizer, ProseFormatter formatter, WorldState state)
        {
            this.enqueue = enqueue;
            this.localizer = localizer;
            text = new ChatText(localizer, formatter, state);
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create conversations on Unity's main thread.");
        }
        public void Start(SwgId npcSwgId)
        {
            if (IsOpen) Stop();
            enqueue("npcConversationStart", "", npcSwgId);
        }
        public void Select(int index)
        {
            if (!IsOpen || Waiting || index < 0 || index >= Options.Count) return;
            enqueue("npcConversationSelect", index.ToString(CultureInfo.InvariantCulture), NpcSwgId);
            Waiting = true;
            Changed?.Invoke();
        }
        public void Stop()
        {
            if (!IsOpen) return;
            enqueue("npcConversationStop", "", NpcSwgId);
            Clear();
        }
        void Clear()
        {
            NpcSwgId = default;
            Message = "";
            Options = Array.Empty<string>();
            Waiting = false;
            Changed?.Invoke();
        }
        void Post(Action action) => context.Post(_ => { if (!disposed) action(); }, null);
        public void Receive(StartNpcConversationPacket packet) => Post(() =>
        {
            NpcSwgId = packet.NpcSwgId;
            Message = "";
            Options = Array.Empty<string>();
            Waiting = false;
            Changed?.Invoke();
        });
        public void Receive(NpcConversationPacket packet) => Post(() =>
        {
            if (!IsOpen) return;
            Message = text.Format(packet.Message);
            Options = Array.Empty<string>();
            Waiting = false;
            Changed?.Invoke();
        });
        public void Receive(StringListPacket packet) => Post(() =>
        {
            if (!IsOpen) return;
            Options = packet.Options.Select(localizer.Localize).ToArray();
            Waiting = false;
            Changed?.Invoke();
        });
        public void Receive(StopNpcConversationPacket packet) => Post(() =>
        {
            if (packet.NpcSwgId == NpcSwgId.Value) Clear();
        });
        public void Dispose() => disposed = true;
    }
}
