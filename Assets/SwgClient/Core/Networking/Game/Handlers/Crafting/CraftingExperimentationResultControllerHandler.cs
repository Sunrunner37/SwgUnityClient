using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1090 (CraftingExperimentationResult).
    [Preserve]
    internal sealed class CraftingExperimentationResultControllerHandler : ObjectControllerHandler<CraftingExperimentationResultPacket>
    {
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CraftingExperimentationResultPacket.ControllerType;

        [Preserve]
        public CraftingExperimentationResultControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CraftingExperimentationResultPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CraftingExperimentationResultPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
