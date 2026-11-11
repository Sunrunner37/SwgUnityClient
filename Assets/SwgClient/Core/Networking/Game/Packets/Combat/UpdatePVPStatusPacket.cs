using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/tangible/UpdatePVPStatusMessage.h:11 (UpdatePVPStatusMessage).
    public readonly struct UpdatePVPStatusPacket : IPacket
    {
        public const uint Opcode = 0x8a1c126;
        public readonly ushort OperandCount;
        public readonly uint PvpStatus;
        public readonly uint Faction;
        public readonly ulong SwgId;
        UpdatePVPStatusPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected UpdatePVPStatusPacket header");
            PvpStatus = reader.U32();
            Faction = reader.U32();
            SwgId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static UpdatePVPStatusPacket ReadFrom(ByteReader reader) => new UpdatePVPStatusPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PvpStatus);
            writer.Write(Faction);
            writer.Write(SwgId);
        }
    }
}
