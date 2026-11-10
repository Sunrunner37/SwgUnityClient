using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/SpatialChat.h:12 (SpatialChat).
    public readonly struct SpatialChatPacket : IPacket
    {
        public const uint ControllerType = 0xf4;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong SenderId;
        public readonly ulong ChatTargetId;
        public readonly string Message;
        public readonly ushort Volume;
        public readonly ushort ChatType;
        public readonly ushort MoodType;
        public readonly byte ChatFlags;
        public readonly byte LanguageId;
        public readonly ChatParameterBlock Parameters;
        public readonly uint[] Reserved;
        SpatialChatPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected SpatialChatPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            SenderId = reader.U64();
            ChatTargetId = reader.U64();
            Message = reader.Unicode();
            Volume = reader.U16();
            ChatType = reader.U16();
            MoodType = reader.U16();
            ChatFlags = reader.Byte();
            LanguageId = reader.Byte();
            Parameters = ChatParameterBlock.ReadFrom(reader);
            var reserved = new List<uint>();
            while (reader.Remaining > 0)
                reserved.Add(reader.U32());
            Reserved = reserved.ToArray();
            PacketFieldCodec.End(reader);
        }

        public static SpatialChatPacket ReadFrom(ObjectControllerPacket packet) => new SpatialChatPacket(packet);
        public static SpatialChatPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(SenderId);
            writer.Write(ChatTargetId);
            PacketFieldCodec.Unicode(writer, Message);
            writer.Write(Volume);
            writer.Write(ChatType);
            writer.Write(MoodType);
            writer.Write(ChatFlags);
            writer.Write(LanguageId);
            Parameters.WriteTo(writer);
            foreach (var item in Reserved)
                writer.Write(item);
        }
    }
}
