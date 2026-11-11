using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAcceptCallback.h:80 (MissionAcceptResponse).
    public readonly struct MissionAcceptResponsePacket : IPacket
    {
        public const uint ControllerType = 0xfa;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong MissionSwgId;
        public readonly byte Result;
        public readonly byte TerminalIndex;
        MissionAcceptResponsePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected MissionAcceptResponsePacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            MissionSwgId = reader.U64();
            Result = reader.Byte();
            TerminalIndex = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static MissionAcceptResponsePacket ReadFrom(ObjectControllerPacket packet) => new MissionAcceptResponsePacket(packet);
        public static MissionAcceptResponsePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(MissionSwgId);
            writer.Write(Result);
            writer.Write(TerminalIndex);
        }
    }
}
