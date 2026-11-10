using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/LaunchBrowserMessage.h:13 (LaunchBrowserMessage).
    public readonly struct LaunchBrowserPacket : IPacket
    {
        public const uint Opcode = 0x8de7e213;
        public readonly ushort OperandCount;
        public readonly string Url;
        LaunchBrowserPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected LaunchBrowserPacket header");
            Url = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static LaunchBrowserPacket ReadFrom(ByteReader reader) => new LaunchBrowserPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Url);
        }
    }
}
