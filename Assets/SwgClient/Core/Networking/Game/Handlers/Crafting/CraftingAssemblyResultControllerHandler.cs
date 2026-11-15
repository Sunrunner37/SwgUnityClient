using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.Networking;
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
        readonly Lazy<ClientCrafting> crafting;
        readonly GameReplication replication;
        public override uint ControllerType => CraftingAssemblyResultPacket.ControllerType;

        [Preserve]
        public CraftingAssemblyResultControllerHandler(Lazy<ClientCrafting> crafting, GameReplication replication) : base(CraftingAssemblyResultPacket.ReadFrom)
        {
            this.crafting = crafting;
            this.replication = replication;
        }

        protected override void Handle(CraftingAssemblyResultPacket packet)
        {
            replication.UpdatePlayer(_ => crafting.Value.Receive(packet));
        }
    }
}
