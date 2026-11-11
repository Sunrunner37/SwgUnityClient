using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct SuiPageData
    {
        public readonly uint Id;
        public readonly string RootPage;
        public readonly SuiCommandData[] Commands;
        public readonly ulong TargetSwgId;
        public readonly float ForceCloseDistance;
        public readonly ulong UnknownId;
        public readonly uint? ExtraFooterWord;
        SuiPageData(ByteReader reader)
        {
            this = default;
            Id = reader.U32();
            RootPage = reader.Ascii();
            Commands = new SuiCommandData[PacketFieldCodec.Count(reader, reader.U32(), 9)];
            for (int i = 0; i < Commands.Length; i++)
                Commands[i] = SuiCommandData.ReadFrom(reader);
            TargetSwgId = reader.U64();
            ForceCloseDistance = reader.Float();
            UnknownId = reader.U64();
            ExtraFooterWord = reader.Remaining == 0 ? (uint? )null : reader.U32();
        }

        public static SuiPageData ReadFrom(ByteReader reader) => new SuiPageData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Id);
            PacketFieldCodec.Ascii(writer, RootPage);
            writer.Write(checked((uint)Commands.Length));
            foreach (var item in Commands)
                item.WriteTo(writer);
            writer.Write(TargetSwgId);
            writer.Write(ForceCloseDistance);
            writer.Write(UnknownId);
            if (ExtraFooterWord.HasValue)
                writer.Write(ExtraFooterWord.Value);
        }
    }
}
