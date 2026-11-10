using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct RadialMenuEntry
    {
        public readonly byte Index;
        public readonly byte ParentIndex;
        public readonly byte RadialId;
        public readonly byte Callback;
        public readonly string Text;
        RadialMenuEntry(ByteReader reader)
        {
            this = default;
            Index = reader.Byte();
            ParentIndex = reader.Byte();
            RadialId = reader.Byte();
            Callback = reader.Byte();
            Text = reader.Unicode();
        }

        public static RadialMenuEntry ReadFrom(ByteReader reader) => new RadialMenuEntry(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(ParentIndex);
            writer.Write(RadialId);
            writer.Write(Callback);
            PacketFieldCodec.Unicode(writer, Text);
        }
    }
}
