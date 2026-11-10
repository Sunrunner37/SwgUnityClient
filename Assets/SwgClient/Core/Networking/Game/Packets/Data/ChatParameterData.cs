using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    // Core3 server/chat/ChatParameter.cpp, StringIdChatParameter.cpp and WaypointChatParameter.cpp.
    public readonly struct ChatParameterData
    {
        public readonly ushort Padding;
        public readonly byte Type;
        public readonly uint Marker;
        public readonly LocalizedChatString StringId;
        public readonly ChatWaypoint Waypoint;
        public readonly byte TrailingPadding;
        ChatParameterData(ByteReader reader)
        {
            this = default;
            Padding = reader.U16();
            Type = reader.Byte();
            Marker = reader.U32();
            if (Padding > 1)
                throw new InvalidDataException("Invalid chat parameter alignment");
            if (Type == 1 && Marker == 0xffffffff)
                StringId = LocalizedChatString.ReadFrom(reader);
            else if (Type == 4 && Marker == 0xfffffffd)
                Waypoint = ChatWaypoint.ReadFrom(reader);
            else
                throw new InvalidDataException("Unknown chat parameter type");
            if (Padding == 1)
                TrailingPadding = reader.Byte();
        }

        public static ChatParameterData ReadFrom(ByteReader reader) => new ChatParameterData(reader);
        internal static bool IsHeader(ByteReader reader, int offset = 0)
        {
            int p = reader.Position + offset;
            if (reader.Remaining < offset + 7)
                return false;
            byte type = reader.Bytes[p + 2];
            uint marker = ByteReader.At(reader.Bytes, p + 3);
            return (type == 1 && marker == 0xffffffff) || (type == 4 && marker == 0xfffffffd);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Padding);
            writer.Write(Type);
            writer.Write(Marker);
            if (Type == 1)
                StringId.WriteTo(writer);
            else
                Waypoint.WriteTo(writer);
            if (Padding == 1)
                writer.Write(TrailingPadding);
        }
    }
}
