using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/LogoutMessage.h:11 (LogoutMessage).
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ClientLogout.h:10 (ClientLogout).
    public readonly struct LogoutPacket : IPacket
    {
        public const uint Opcode = 0x42fd19dd;
        public readonly ushort OperandCount;
        LogoutPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected LogoutPacket header");
            PacketFieldCodec.End(reader);
        }

        public static LogoutPacket ReadFrom(ByteReader reader) => new LogoutPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
