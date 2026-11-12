using Swg.Client.Core.Networking.Game.Packets.Chat;
using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/SpatialChat.h:12 (SpatialChat).
    [Preserve]
    internal sealed class SpatialChatControllerHandler : ObjectControllerHandler<SpatialChatPacket>
    {
        readonly ClientChat chat;
        public override uint ControllerType => SpatialChatPacket.ControllerType;

        [Preserve]
        public SpatialChatControllerHandler(ClientChat chat) : base(SpatialChatPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(SpatialChatPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
