using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatAction.h:11 (CombatAction).
    [Preserve]
    internal sealed class CombatActionControllerHandler : ObjectControllerHandler<CombatActionPacket>
    {
        public override uint ControllerType => CombatActionPacket.ControllerType;

        readonly ClientCombat combat;
        readonly GameReplication replication;
        [Preserve]
        public CombatActionControllerHandler(ClientCombat combat, GameReplication replication) : base(CombatActionPacket.ReadFrom)
        {
            this.combat = combat;
            this.replication = replication;
        }
        protected override void Handle(CombatActionPacket packet)
        {
            replication.UpdateCombatPostures(packet);
            combat.Receive(packet);
        }
    }
}
