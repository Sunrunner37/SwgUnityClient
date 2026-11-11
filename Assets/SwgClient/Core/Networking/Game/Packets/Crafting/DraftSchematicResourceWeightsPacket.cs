using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/draftschematic/DraftSchematicImplementation.cpp:76 (DraftSchematicResourceWeights).
    public readonly struct DraftSchematicResourceWeightsPacket : IPacket
    {
        public const uint ControllerType = 0x207;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint ClientCrc;
        public readonly uint ServerCrc;
        public readonly ResourceWeightGroup[] BatchWeights;
        public readonly ResourceWeightGroup[] Weights;
        DraftSchematicResourceWeightsPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected DraftSchematicResourceWeightsPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ClientCrc = reader.U32();
            ServerCrc = reader.U32();
            BatchWeights = new ResourceWeightGroup[PacketFieldCodec.Count(reader, reader.Byte(), 1)];
            for (int i = 0; i < BatchWeights.Length; i++)
                BatchWeights[i] = ResourceWeightGroup.ReadFrom(reader);
            Weights = new ResourceWeightGroup[PacketFieldCodec.Count(reader, reader.Byte(), 1)];
            for (int i = 0; i < Weights.Length; i++)
                Weights[i] = ResourceWeightGroup.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static DraftSchematicResourceWeightsPacket ReadFrom(ObjectControllerPacket packet) => new DraftSchematicResourceWeightsPacket(packet);
        public static DraftSchematicResourceWeightsPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(ClientCrc);
            writer.Write(ServerCrc);
            writer.Write(checked((byte)BatchWeights.Length));
            foreach (var item in BatchWeights)
                item.WriteTo(writer);
            writer.Write(checked((byte)Weights.Length));
            foreach (var item in Weights)
                item.WriteTo(writer);
        }
    }
}
