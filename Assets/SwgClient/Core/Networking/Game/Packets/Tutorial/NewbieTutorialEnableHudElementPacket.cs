using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Tutorial
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/NewbieTutorialEnableHudElement.h:19 (NewbieTutorialEnableHudElement).
    public readonly struct NewbieTutorialEnableHudElementPacket : IPacket
    {
        public const uint Opcode = 0xca375124;
        public readonly ushort OperandCount;
        public readonly string Element;
        public readonly byte Enable;
        public readonly float BlinkCount;
        NewbieTutorialEnableHudElementPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected NewbieTutorialEnableHudElementPacket header");
            Element = reader.Ascii();
            Enable = reader.Byte();
            BlinkCount = reader.Float();
            PacketFieldCodec.End(reader);
        }

        public static NewbieTutorialEnableHudElementPacket ReadFrom(ByteReader reader) => new NewbieTutorialEnableHudElementPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Element);
            writer.Write(Enable);
            writer.Write(BlinkCount);
        }
    }
}
