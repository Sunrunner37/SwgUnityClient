using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:477 (CraftingIngredientSlots).
    [Preserve]
    internal sealed class CraftingIngredientSlotsControllerHandler : ObjectControllerHandler<CraftingIngredientSlotsPacket>
    {
        public override uint ControllerType => CraftingIngredientSlotsPacket.ControllerType;

        [Preserve]
        public CraftingIngredientSlotsControllerHandler() : base(CraftingIngredientSlotsPacket.ReadFrom)
        {
        }

        protected override void Handle(CraftingIngredientSlotsPacket packet)
        {
            throw new NotImplementedException($"CraftingIngredientSlots (controller 0x{ControllerType:X8})");
        }
    }
}
