using Swg.Client.Core.Networking.Game.Packets.Data;
using System.IO;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    /// <summary>Core3 SuiEventNotificationCallback: the page event and subscribed property values.</summary>
    public readonly struct SuiEventNotificationPacket : IPacket
    {
        public const uint Opcode = 0x092d3564;
        public readonly uint PageId;
        public readonly uint EventIndex;
        public readonly string[] Arguments;
        public SuiEventNotificationPacket(uint pageId, uint eventIndex, params string[] arguments)
        {
            PageId = pageId;
            EventIndex = eventIndex;
            Arguments = arguments;
        }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)2);
            writer.Write(Opcode);
            writer.Write(PageId);
            writer.Write(EventIndex);
            writer.Write((uint)Arguments.Length);
            writer.Write(0u);
            foreach (var argument in Arguments)
                PacketFieldCodec.Unicode(writer, argument);
        }
    }
}

