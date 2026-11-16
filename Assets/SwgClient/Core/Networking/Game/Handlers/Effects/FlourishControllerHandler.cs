using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Flourish.h:10 (Flourish).
    [Preserve]
    internal sealed class FlourishControllerHandler : ObjectControllerHandler<FlourishPacket>
    {
        public override uint ControllerType => FlourishPacket.ControllerType;

        readonly ClientCombat combat;
        [Preserve]
        public FlourishControllerHandler(ClientCombat combat) : base(FlourishPacket.ReadFrom) => this.combat = combat;

        protected override void Handle(FlourishPacket packet)
        {
            combat.Receive(packet);
        }
    }
}
