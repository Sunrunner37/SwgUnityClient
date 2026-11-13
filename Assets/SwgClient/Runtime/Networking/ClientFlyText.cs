using System;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Runtime.Localization;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Delivers localized server fly text to the UI on Unity's main thread.</summary>
    public sealed class ClientFlyText : IDisposable
    {
        public readonly struct Message
        {
            public readonly SwgId SwgId;
            public readonly string Text;
            public readonly Color Color;
            public readonly float Scale;
            public Message(SwgId swgId, string text, Color color, float scale)
            {
                SwgId = swgId;
                Text = text;
                Color = color;
                Scale = scale;
            }
        }
        readonly StringTableLocalizer localizer;
        readonly SynchronizationContext context;
        public event Action<Message> Received;
        public ClientFlyText(StringTableLocalizer localizer)
        {
            this.localizer = localizer;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create ClientFlyText on Unity's main thread.");
        }
        public void Receive(ShowFlyTextPacket packet) => context.Post(_ =>
        {
            var message = new Message(packet.SwgId, localizer.Lookup(packet.StringId.File, packet.StringId.Key),
                new Color32(packet.Red, packet.Green, packet.Blue, 255), packet.Size > 0 ? packet.Size : 1);
            Received?.Invoke(message);
        }, null);
        public void Dispose() => Received = null;
    }
}
