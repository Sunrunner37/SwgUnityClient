using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:898 (CraftingAssemblyResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:955 (CraftingAssemblyResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1237 (CraftingAssemblyResult).
    [Preserve]
    internal sealed class CraftingAssemblyResultControllerHandler : ObjectControllerHandler<CraftingAssemblyResultPacket>
    {
        public override uint ControllerType => CraftingAssemblyResultPacket.ControllerType;

        [Preserve]
        public CraftingAssemblyResultControllerHandler() : base(CraftingAssemblyResultPacket.ReadFrom)
        {
        }

        protected override void Handle(CraftingAssemblyResultPacket packet)
        {
            throw new NotImplementedException($"CraftingAssemblyResult (controller 0x{ControllerType:X8})");
        }
    }
}
