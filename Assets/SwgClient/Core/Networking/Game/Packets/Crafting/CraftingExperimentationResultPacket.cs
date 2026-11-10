using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1090 (CraftingExperimentationResult).
    public readonly struct CraftingExperimentationResultPacket : IPacket
    {
        public const uint ControllerType = 0x113;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly uint Message;
        public readonly uint Result;
        public readonly byte Counter;
        CraftingExperimentationResultPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CraftingExperimentationResultPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Message = reader.U32();
            Result = reader.U32();
            Counter = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static CraftingExperimentationResultPacket ReadFrom(ObjectControllerPacket packet) => new CraftingExperimentationResultPacket(packet);
        public static CraftingExperimentationResultPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(Message);
            writer.Write(Result);
            writer.Write(Counter);
        }
    }
}
