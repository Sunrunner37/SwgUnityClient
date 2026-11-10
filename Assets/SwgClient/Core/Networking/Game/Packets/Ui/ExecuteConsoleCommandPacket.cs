using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ExecuteConsoleCommand.h:11 (ExecuteConsoleCommand).
    public readonly struct ExecuteConsoleCommandPacket : IPacket
    {
        public const uint Opcode = 0xb1cfce1c;
        public readonly ushort OperandCount;
        public readonly string Command;
        ExecuteConsoleCommandPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected ExecuteConsoleCommandPacket header");
            Command = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ExecuteConsoleCommandPacket ReadFrom(ByteReader reader) => new ExecuteConsoleCommandPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Command);
        }
    }
}
