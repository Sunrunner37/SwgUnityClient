using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct DraftSlotData
    {
        public readonly StringIdReference Name;
        public readonly byte Optional;
        public readonly uint OptionCount;
        public readonly StringIdReference IngredientName;
        public readonly string ResourceType;
        public readonly byte Type;
        public readonly uint Quantity;
        public readonly ushort Reserved;
        DraftSlotData(ByteReader reader)
        {
            this = default;
            Name = StringIdReference.ReadFrom(reader);
            Optional = reader.Byte();
            OptionCount = reader.U32();
            IngredientName = StringIdReference.ReadFrom(reader);
            ResourceType = reader.Unicode();
            Type = reader.Byte();
            Quantity = reader.U32();
            if (Type == 2)
            {
                Reserved = reader.U16();
            }
        }

        public static DraftSlotData ReadFrom(ByteReader reader) => new DraftSlotData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            Name.WriteTo(writer);
            writer.Write(Optional);
            writer.Write(OptionCount);
            IngredientName.WriteTo(writer);
            PacketFieldCodec.Unicode(writer, ResourceType);
            writer.Write(Type);
            writer.Write(Quantity);
            if (Type == 2)
            {
                writer.Write(Reserved);
            }
        }
    }
}
