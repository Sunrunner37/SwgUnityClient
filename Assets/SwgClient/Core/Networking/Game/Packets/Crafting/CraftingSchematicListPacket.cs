using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:154 (CraftingSchematicList).
    public readonly struct CraftingSchematicListPacket : IPacket
    {
        public const uint ControllerType = 0x102;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong ToolSwgId;
        public readonly ulong StationId;
        public readonly SchematicChoice[] Schematics;
        CraftingSchematicListPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CraftingSchematicListPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ToolSwgId = reader.U64();
            StationId = reader.U64();
            Schematics = new SchematicChoice[PacketFieldCodec.Count(reader, reader.U32(), 12)];
            for (int i = 0; i < Schematics.Length; i++)
                Schematics[i] = SchematicChoice.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static CraftingSchematicListPacket ReadFrom(ObjectControllerPacket packet) => new CraftingSchematicListPacket(packet);
        public static CraftingSchematicListPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(ToolSwgId);
            writer.Write(StationId);
            writer.Write(checked((uint)Schematics.Length));
            foreach (var item in Schematics)
                item.WriteTo(writer);
        }
    }
}
