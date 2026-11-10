using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ChatAvatar
    {
        public readonly string Game;
        public readonly string Galaxy;
        public readonly string Name;
        ChatAvatar(ByteReader reader)
        {
            this = default;
            Game = reader.Ascii();
            Galaxy = reader.Ascii();
            Name = reader.Ascii();
        }

        public static ChatAvatar ReadFrom(ByteReader reader) => new ChatAvatar(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Ascii(writer, Game);
            PacketFieldCodec.Ascii(writer, Galaxy);
            PacketFieldCodec.Ascii(writer, Name);
        }
    }
}
