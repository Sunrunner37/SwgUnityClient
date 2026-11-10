using Swg.Client.Core.Networking.Game.Packets.Conversation;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StringList.h:10 (StringList).
    [Preserve]
    internal sealed class StringListControllerHandler : ObjectControllerHandler<StringListPacket>
    {
        public override uint ControllerType => StringListPacket.ControllerType;

        [Preserve]
        public StringListControllerHandler() : base(StringListPacket.ReadFrom)
        {
        }

        protected override void Handle(StringListPacket packet)
        {
            throw new NotImplementedException($"StringList (controller 0x{ControllerType:X8})");
        }
    }
}
