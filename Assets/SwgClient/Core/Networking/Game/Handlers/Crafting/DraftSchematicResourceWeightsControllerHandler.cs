using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/draftschematic/DraftSchematicImplementation.cpp:76 (DraftSchematicResourceWeights).
    [Preserve]
    internal sealed class DraftSchematicResourceWeightsControllerHandler : ObjectControllerHandler<DraftSchematicResourceWeightsPacket>
    {
        public override uint ControllerType => DraftSchematicResourceWeightsPacket.ControllerType;

        [Preserve]
        public DraftSchematicResourceWeightsControllerHandler() : base(DraftSchematicResourceWeightsPacket.ReadFrom)
        {
        }

        protected override void Handle(DraftSchematicResourceWeightsPacket packet)
        {
            throw new NotImplementedException($"DraftSchematicResourceWeights (controller 0x{ControllerType:X8})");
        }
    }
}
