using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ClientMfdStatusUpdateMessage.h:12 (ClientMfdStatusUpdateMessage).
    public readonly struct ClientMfdStatusUpdatePacket : IPacket
    {
        public const uint Opcode = 0x2d2d6ee1;
        public readonly ushort OperandCount;
        public readonly MfdMember[] Members;
        ClientMfdStatusUpdatePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ClientMfdStatusUpdatePacket header");
            var members = new List<MfdMember>();
            while (reader.Remaining > 0)
                members.Add(MfdMember.ReadFrom(reader));
            Members = members.ToArray();
            PacketFieldCodec.End(reader);
        }

        public static ClientMfdStatusUpdatePacket ReadFrom(ByteReader reader) => new ClientMfdStatusUpdatePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            foreach (var item in Members)
                item.WriteTo(writer);
        }
    }
}
