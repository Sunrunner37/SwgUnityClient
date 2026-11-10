using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Tutorial
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/NewbieTutorialRequest.h:31 (NewbieTutorialRequest).
    public readonly struct NewbieTutorialRequestPacket : IPacket
    {
        public const uint Opcode = 0x90dd61af;
        public readonly ushort OperandCount;
        public readonly string Action;
        NewbieTutorialRequestPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected NewbieTutorialRequestPacket header");
            Action = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static NewbieTutorialRequestPacket ReadFrom(ByteReader reader) => new NewbieTutorialRequestPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Action);
        }
    }
}
