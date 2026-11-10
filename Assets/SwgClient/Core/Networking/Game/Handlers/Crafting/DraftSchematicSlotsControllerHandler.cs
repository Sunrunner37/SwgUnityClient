using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/draftschematic/DraftSchematicImplementation.cpp:43 (DraftSchematicSlots).
    [Preserve]
    internal sealed class DraftSchematicSlotsControllerHandler : ObjectControllerHandler<DraftSchematicSlotsPacket>
    {
        public override uint ControllerType => DraftSchematicSlotsPacket.ControllerType;

        [Preserve]
        public DraftSchematicSlotsControllerHandler() : base(DraftSchematicSlotsPacket.ReadFrom)
        {
        }

        protected override void Handle(DraftSchematicSlotsPacket packet)
        {
            throw new NotImplementedException($"DraftSchematicSlots (controller 0x{ControllerType:X8})");
        }
    }
}
