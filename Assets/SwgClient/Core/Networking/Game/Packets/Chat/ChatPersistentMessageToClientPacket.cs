using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatPersistentMessageToClient.h:13 (ChatPersistentMessageToClient).
    public readonly struct ChatPersistentMessageToClientPacket : IPacket
    {
        public const uint Opcode = 0x8485e17;
        public readonly ushort OperandCount;
        public readonly string Sender;
        public readonly string Game;
        public readonly string Galaxy;
        public readonly uint MailId;
        public readonly byte MessageType;
        public readonly string Body;
        public readonly string Subject;
        public readonly ChatParameterBlock Parameters;
        public readonly byte Status;
        public readonly uint Timestamp;
        public readonly uint? Reserved;
        ChatPersistentMessageToClientPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ChatPersistentMessageToClientPacket header");
            Sender = reader.Ascii();
            Game = reader.Ascii();
            Galaxy = reader.Ascii();
            MailId = reader.U32();
            MessageType = reader.Byte();
            Body = reader.Unicode();
            Subject = reader.Unicode();
            Parameters = ChatParameterBlock.ReadFrom(reader);
            Status = reader.Byte();
            Timestamp = reader.U32();
            Reserved = reader.Remaining == 0 ? (uint? )null : reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatPersistentMessageToClientPacket ReadFrom(ByteReader reader) => new ChatPersistentMessageToClientPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Sender);
            PacketFieldCodec.Ascii(writer, Game);
            PacketFieldCodec.Ascii(writer, Galaxy);
            writer.Write(MailId);
            writer.Write(MessageType);
            PacketFieldCodec.Unicode(writer, Body);
            PacketFieldCodec.Unicode(writer, Subject);
            Parameters.WriteTo(writer);
            writer.Write(Status);
            writer.Write(Timestamp);
            if (Reserved.HasValue)
                writer.Write(Reserved.Value);
        }
    }
}
