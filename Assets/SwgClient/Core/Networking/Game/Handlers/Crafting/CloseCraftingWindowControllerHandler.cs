using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:294 (CloseCraftingWindow).
    [Preserve]
    internal sealed class CloseCraftingWindowControllerHandler : ObjectControllerHandler<CloseCraftingWindowPacket>
    {
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CloseCraftingWindowPacket.ControllerType;

        [Preserve]
        public CloseCraftingWindowControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CloseCraftingWindowPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CloseCraftingWindowPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
