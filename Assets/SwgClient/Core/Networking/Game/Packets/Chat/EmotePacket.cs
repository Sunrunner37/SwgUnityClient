using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Emote.h:10 (Emote).
    public readonly struct EmotePacket : IPacket
    {
        public const uint ControllerType = 0x12e;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong SenderId;
        public readonly ulong TargetId;
        public readonly uint EmoteId;
        public readonly byte DisplayFlags;
        EmotePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected EmotePacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            SenderId = reader.U64();
            TargetId = reader.U64();
            EmoteId = reader.U32();
            DisplayFlags = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static EmotePacket ReadFrom(ObjectControllerPacket packet) => new EmotePacket(packet);
        public static EmotePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(SenderId);
            writer.Write(TargetId);
            writer.Write(EmoteId);
            writer.Write(DisplayFlags);
        }
    }
}
