using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Characters;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/WeaponRanges.h:14 (WeaponRanges).
    [Preserve]
    internal sealed class WeaponRangesControllerHandler : ObjectControllerHandler<WeaponRangesPacket>
    {
        readonly GameReplication replication;
        public override uint ControllerType => WeaponRangesPacket.ControllerType;

        [Preserve]
        public WeaponRangesControllerHandler(GameReplication replication) : base(WeaponRangesPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(WeaponRangesPacket packet)
        {
            replication.UpdatePlayer(player => player.WeaponRanges = new WeaponRanges(packet.WeaponSwgId, packet.IdealRange, packet.MaximumRange, packet.PointBlankAccuracy, packet.IdealAccuracy, packet.MaximumRangeAccuracy));
        }
    }
}
