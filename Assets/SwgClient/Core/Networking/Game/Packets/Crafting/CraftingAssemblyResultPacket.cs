using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:898 (CraftingAssemblyResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:955 (CraftingAssemblyResult).
    // Core3/MMOCoreORB/src/server/zone/objects/player/sessions/crafting/CraftingSessionImplementation.cpp:1237 (CraftingAssemblyResult).
    public readonly struct CraftingAssemblyResultPacket : IPacket
    {
        public const uint ControllerType = 0x1be;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint Message;
        public readonly uint Result;
        public readonly byte Counter;
        CraftingAssemblyResultPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CraftingAssemblyResultPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Message = reader.U32();
            Result = reader.U32();
            Counter = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static CraftingAssemblyResultPacket ReadFrom(ObjectControllerPacket packet) => new CraftingAssemblyResultPacket(packet);
        public static CraftingAssemblyResultPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(Message);
            writer.Write(Result);
            writer.Write(Counter);
        }
    }
}
