using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ChatRoomData
    {
        public readonly uint Id;
        public readonly uint Private;
        public readonly byte Moderated;
        public readonly string Path;
        public readonly ChatAvatar Owner;
        public readonly ChatAvatar Creator;
        public readonly string Title;
        public readonly ChatAvatar[] Moderators;
        public readonly ChatAvatar[] Users;
        ChatRoomData(ByteReader reader)
        {
            this = default;
            Id = reader.U32();
            Private = reader.U32();
            Moderated = reader.Byte();
            Path = reader.Ascii();
            Owner = ChatAvatar.ReadFrom(reader);
            Creator = ChatAvatar.ReadFrom(reader);
            Title = reader.Unicode();
            Moderators = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Moderators.Length; i++)
                Moderators[i] = ChatAvatar.ReadFrom(reader);
            Users = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Users.Length; i++)
                Users[i] = ChatAvatar.ReadFrom(reader);
        }

        public static ChatRoomData ReadFrom(ByteReader reader) => new ChatRoomData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Private);
            writer.Write(Moderated);
            PacketFieldCodec.Ascii(writer, Path);
            Owner.WriteTo(writer);
            Creator.WriteTo(writer);
            PacketFieldCodec.Unicode(writer, Title);
            writer.Write(checked((uint)Moderators.Length));
            foreach (var item in Moderators)
                item.WriteTo(writer);
            writer.Write(checked((uint)Users.Length));
            foreach (var item in Users)
                item.WriteTo(writer);
        }
    }
}
