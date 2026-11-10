using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SearchKnowledgebaseResponseMessage.h:10 (SearchKnowledgebaseResponseMessage).
    [Preserve]
    internal sealed class SearchKnowledgebaseResponsePacketHandler : GamePacketHandler<SearchKnowledgebaseResponsePacket>
    {
        public override uint Opcode => SearchKnowledgebaseResponsePacket.Opcode;

        [Preserve]
        public SearchKnowledgebaseResponsePacketHandler() : base(SearchKnowledgebaseResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(SearchKnowledgebaseResponsePacket packet)
        {
            throw new NotImplementedException($"SearchKnowledgebaseResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
