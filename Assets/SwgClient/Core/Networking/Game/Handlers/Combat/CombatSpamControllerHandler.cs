using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatSpam.h:12 (CombatSpam).
    [Preserve]
    internal sealed class CombatSpamControllerHandler : ObjectControllerHandler<CombatSpamPacket>
    {
        public override uint ControllerType => CombatSpamPacket.ControllerType;

        readonly ClientChat chat;
        [Preserve]
        public CombatSpamControllerHandler(ClientChat chat) : base(CombatSpamPacket.ReadFrom) => this.chat = chat;
        protected override void Handle(CombatSpamPacket packet) => chat.Receive(packet);
    }
}
