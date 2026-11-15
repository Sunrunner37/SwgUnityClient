using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
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
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CraftingResultPacket.ControllerType;

        [Preserve]
        public CraftingResultControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CraftingResultPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CraftingResultPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
