using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:154 (CraftingSchematicList).
    [Preserve]
    internal sealed class CraftingSchematicListControllerHandler : ObjectControllerHandler<CraftingSchematicListPacket>
    {
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CraftingSchematicListPacket.ControllerType;

        [Preserve]
        public CraftingSchematicListControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CraftingSchematicListPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CraftingSchematicListPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
