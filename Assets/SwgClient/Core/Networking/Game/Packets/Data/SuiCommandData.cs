using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct SuiCommandData
    {
        public readonly byte Type;
        public readonly string[] WideParameters;
        public readonly string[] NarrowParameters;
        SuiCommandData(ByteReader reader)
        {
            this = default;
            Type = reader.Byte();
            WideParameters = new string[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < WideParameters.Length; i++)
                WideParameters[i] = reader.Unicode();
            NarrowParameters = new string[PacketFieldCodec.Count(reader, reader.U32(), 2)];
            for (int i = 0; i < NarrowParameters.Length; i++)
                NarrowParameters[i] = reader.Ascii();
        }

        public static SuiCommandData ReadFrom(ByteReader reader) => new SuiCommandData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(checked((uint)WideParameters.Length));
            foreach (var item in WideParameters)
                PacketFieldCodec.Unicode(writer, item);
            writer.Write(checked((uint)NarrowParameters.Length));
            foreach (var item in NarrowParameters)
                PacketFieldCodec.Ascii(writer, item);
        }
    }
}
