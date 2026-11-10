using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:294 (CloseCraftingWindow).
    [Preserve]
    internal sealed class CloseCraftingWindowControllerHandler : ObjectControllerHandler<CloseCraftingWindowPacket>
    {
        public override uint ControllerType => CloseCraftingWindowPacket.ControllerType;

        [Preserve]
        public CloseCraftingWindowControllerHandler() : base(CloseCraftingWindowPacket.ReadFrom)
        {
        }

        protected override void Handle(CloseCraftingWindowPacket packet)
        {
            throw new NotImplementedException($"CloseCraftingWindow (controller 0x{ControllerType:X8})");
        }
    }
}
