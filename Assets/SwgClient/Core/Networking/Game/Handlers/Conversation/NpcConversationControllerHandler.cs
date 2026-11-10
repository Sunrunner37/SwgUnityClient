using Swg.Client.Core.Networking.Game.Packets.Conversation;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/NpcConversationMessage.h:14 (NpcConversationMessage).
    [Preserve]
    internal sealed class NpcConversationControllerHandler : ObjectControllerHandler<NpcConversationPacket>
    {
        public override uint ControllerType => NpcConversationPacket.ControllerType;

        [Preserve]
        public NpcConversationControllerHandler() : base(NpcConversationPacket.ReadFrom)
        {
        }

        protected override void Handle(NpcConversationPacket packet)
        {
            throw new NotImplementedException($"NpcConversationMessage (controller 0x{ControllerType:X8})");
        }
    }
}
