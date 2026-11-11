using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Loot
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/OpenLotteryWindow.h:14 (OpenLotteryWindow).
    public readonly struct OpenLotteryWindowPacket : IPacket
    {
        public const uint ControllerType = 0x43c;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong InventorySwgId;
        OpenLotteryWindowPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected OpenLotteryWindowPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            InventorySwgId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static OpenLotteryWindowPacket ReadFrom(ObjectControllerPacket packet) => new OpenLotteryWindowPacket(packet);
        public static OpenLotteryWindowPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(InventorySwgId);
        }
    }
}
