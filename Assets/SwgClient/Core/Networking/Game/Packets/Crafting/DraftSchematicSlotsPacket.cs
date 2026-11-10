using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/draftschematic/DraftSchematicImplementation.cpp:43 (DraftSchematicSlots).
    public readonly struct DraftSchematicSlotsPacket : IPacket
    {
        public const uint ControllerType = 0x1bf;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly uint ClientCrc;
        public readonly uint ServerCrc;
        public readonly int Complexity;
        public readonly int Size;
        public readonly byte Reserved1;
        public readonly DraftSlotData[] Slots;
        public readonly ushort Reserved2;
        DraftSchematicSlotsPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected DraftSchematicSlotsPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ClientCrc = reader.U32();
            ServerCrc = reader.U32();
            Complexity = unchecked((int)reader.U32());
            Size = unchecked((int)reader.U32());
            Reserved1 = reader.Byte();
            Slots = new DraftSlotData[PacketFieldCodec.Count(reader, reader.U32(), 30)];
            for (int i = 0; i < Slots.Length; i++)
                Slots[i] = DraftSlotData.ReadFrom(reader);
            Reserved2 = reader.U16();
            PacketFieldCodec.End(reader);
        }

        public static DraftSchematicSlotsPacket ReadFrom(ObjectControllerPacket packet) => new DraftSchematicSlotsPacket(packet);
        public static DraftSchematicSlotsPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(ClientCrc);
            writer.Write(ServerCrc);
            writer.Write(Complexity);
            writer.Write(Size);
            writer.Write(Reserved1);
            writer.Write(checked((uint)Slots.Length));
            foreach (var item in Slots)
                item.WriteTo(writer);
            writer.Write(Reserved2);
        }
    }
}
