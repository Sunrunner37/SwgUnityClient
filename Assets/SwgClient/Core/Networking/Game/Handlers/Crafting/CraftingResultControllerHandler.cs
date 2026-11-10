using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/tangible/tool/CraftingToolImplementation.cpp:155 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:278 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:285 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:309 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:625 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1216 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1259 (CraftingResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1370 (CraftingResult).
    [Preserve]
    internal sealed class CraftingResultControllerHandler : ObjectControllerHandler<CraftingResultPacket>
    {
        public override uint ControllerType => CraftingResultPacket.ControllerType;

        [Preserve]
        public CraftingResultControllerHandler() : base(CraftingResultPacket.ReadFrom)
        {
        }

        protected override void Handle(CraftingResultPacket packet)
        {
            throw new NotImplementedException($"CraftingResult (controller 0x{ControllerType:X8})");
        }
    }
}
