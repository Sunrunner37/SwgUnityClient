using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:477 (CraftingIngredientSlots).
    public readonly struct CraftingIngredientSlotsPacket : IPacket
    {
        public const uint ControllerType = 0x103;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong ToolId;
        public readonly ulong ManufactureSchematicId;
        public readonly ulong PrototypeId;
        public readonly uint Reserved1;
        public readonly byte AllowFactory;
        public readonly DraftSlotData[] Slots;
        public readonly ushort Reserved2;
        CraftingIngredientSlotsPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CraftingIngredientSlotsPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ToolId = reader.U64();
            ManufactureSchematicId = reader.U64();
            PrototypeId = reader.U64();
            Reserved1 = reader.U32();
            AllowFactory = reader.Byte();
            Slots = new DraftSlotData[PacketFieldCodec.Count(reader, reader.U32(), 30)];
            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = DraftSlotData.ReadFrom(reader);
            Reserved2 = reader.U16();
            PacketFieldCodec.End(reader);
        }

        public static CraftingIngredientSlotsPacket ReadFrom(ObjectControllerPacket packet) => new CraftingIngredientSlotsPacket(packet);
        public static CraftingIngredientSlotsPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(ToolId);
            writer.Write(ManufactureSchematicId);
            writer.Write(PrototypeId);
            writer.Write(Reserved1);
            writer.Write(AllowFactory);
            writer.Write(checked((uint)Slots.Length));
            foreach (var item in Slots)
                item.WriteTo(writer);
            writer.Write(Reserved2);
        }
    }
}
