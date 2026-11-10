using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:154 (CraftingSchematicList).
    [Preserve]
    internal sealed class CraftingSchematicListControllerHandler : ObjectControllerHandler<CraftingSchematicListPacket>
    {
        public override uint ControllerType => CraftingSchematicListPacket.ControllerType;

        [Preserve]
        public CraftingSchematicListControllerHandler() : base(CraftingSchematicListPacket.ReadFrom)
        {
        }

        protected override void Handle(CraftingSchematicListPacket packet)
        {
            throw new NotImplementedException($"CraftingSchematicList (controller 0x{ControllerType:X8})");
        }
    }
}
