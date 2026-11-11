using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/IsVendorOwnerResponseMessage.h:13 (IsVendorOwnerResponseMessage).
    public readonly struct IsVendorOwnerResponsePacket : IPacket
    {
        public const uint Opcode = 0xce04173e;
        public readonly ushort OperandCount;
        public readonly uint Rights;
        public readonly uint ErrorCode;
        public readonly ulong VendorSwgId;
        public readonly string VendorUid;
        public readonly ushort Reserved;
        IsVendorOwnerResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected IsVendorOwnerResponsePacket header");
            Rights = reader.U32();
            ErrorCode = reader.U32();
            VendorSwgId = reader.U64();
            VendorUid = reader.Ascii();
            Reserved = reader.U16();
            PacketFieldCodec.End(reader);
        }

        public static IsVendorOwnerResponsePacket ReadFrom(ByteReader reader) => new IsVendorOwnerResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Rights);
            writer.Write(ErrorCode);
            writer.Write(VendorSwgId);
            PacketFieldCodec.Ascii(writer, VendorUid);
            writer.Write(Reserved);
        }
    }
}
