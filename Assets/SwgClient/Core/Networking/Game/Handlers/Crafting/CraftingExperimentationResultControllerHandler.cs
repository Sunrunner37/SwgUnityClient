using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1090 (CraftingExperimentationResult).
    [Preserve]
    internal sealed class CraftingExperimentationResultControllerHandler : ObjectControllerHandler<CraftingExperimentationResultPacket>
    {
        public override uint ControllerType => CraftingExperimentationResultPacket.ControllerType;

        [Preserve]
        public CraftingExperimentationResultControllerHandler() : base(CraftingExperimentationResultPacket.ReadFrom)
        {
        }

        protected override void Handle(CraftingExperimentationResultPacket packet)
        {
            throw new NotImplementedException($"CraftingExperimentationResult (controller 0x{ControllerType:X8})");
        }
    }
}
