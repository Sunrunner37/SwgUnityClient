using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:477 (CraftingIngredientSlots).
    [Preserve]
    internal sealed class CraftingIngredientSlotsControllerHandler : ObjectControllerHandler<CraftingIngredientSlotsPacket>
    {
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CraftingIngredientSlotsPacket.ControllerType;

        [Preserve]
        public CraftingIngredientSlotsControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CraftingIngredientSlotsPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CraftingIngredientSlotsPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
