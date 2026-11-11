using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.ImageDesign
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ImageDesignMessage.h:11 (ImageDesignStartMessage).
    public readonly struct ImageDesignStartPacket : IPacket
    {
        public const uint ControllerType = 0x23a;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong DesignerSwgId;
        public readonly ulong ClientSwgId;
        public readonly ulong TentSwgId;
        public readonly string Holoemote;
        ImageDesignStartPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected ImageDesignStartPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            DesignerSwgId = reader.U64();
            ClientSwgId = reader.U64();
            TentSwgId = reader.U64();
            Holoemote = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ImageDesignStartPacket ReadFrom(ObjectControllerPacket packet) => new ImageDesignStartPacket(packet);
        public static ImageDesignStartPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(DesignerSwgId);
            writer.Write(ClientSwgId);
            writer.Write(TentSwgId);
            PacketFieldCodec.Ascii(writer, Holoemote);
        }
    }
}
