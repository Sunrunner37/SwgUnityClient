using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ServerWeatherMessage.h:10 (ServerWeatherMessage).
    public readonly struct ServerWeatherPacket : IPacket
    {
        public const uint Opcode = 0x486356ea;
        public readonly ushort OperandCount;
        public readonly int Weather;
        public readonly float WindX;
        public readonly float WindMagnitude;
        public readonly float WindY;
        ServerWeatherPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ServerWeatherPacket header");
            Weather = unchecked((int)reader.U32());
            WindX = reader.Float();
            WindMagnitude = reader.Float();
            WindY = reader.Float();
            PacketFieldCodec.End(reader);
        }

        public static ServerWeatherPacket ReadFrom(ByteReader reader) => new ServerWeatherPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Weather);
            writer.Write(WindX);
            writer.Write(WindMagnitude);
            writer.Write(WindY);
        }
    }
}
