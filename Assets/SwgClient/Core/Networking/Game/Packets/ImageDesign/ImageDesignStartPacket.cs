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
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong DesignerId;
        public readonly ulong ClientId;
        public readonly ulong TentId;
        public readonly string Holoemote;
        ImageDesignStartPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected ImageDesignStartPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            DesignerId = reader.U64();
            ClientId = reader.U64();
            TentId = reader.U64();
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
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(DesignerId);
            writer.Write(ClientId);
            writer.Write(TentId);
            PacketFieldCodec.Ascii(writer, Holoemote);
        }
    }
}
