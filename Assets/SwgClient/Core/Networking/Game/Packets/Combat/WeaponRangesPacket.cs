using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/WeaponRanges.h:14 (WeaponRanges).
    public readonly struct WeaponRangesPacket : IPacket
    {
        public const uint ControllerType = 0x140;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong WeaponSwgId;
        public readonly float IdealRange;
        public readonly float MaximumRange;
        public readonly int PointBlankAccuracy;
        public readonly int IdealAccuracy;
        public readonly int MaximumRangeAccuracy;
        WeaponRangesPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected WeaponRangesPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            WeaponSwgId = reader.U64();
            IdealRange = reader.Float();
            MaximumRange = reader.Float();
            PointBlankAccuracy = unchecked((int)reader.U32());
            IdealAccuracy = unchecked((int)reader.U32());
            MaximumRangeAccuracy = unchecked((int)reader.U32());
            PacketFieldCodec.End(reader);
        }

        public static WeaponRangesPacket ReadFrom(ObjectControllerPacket packet) => new WeaponRangesPacket(packet);
        public static WeaponRangesPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(WeaponSwgId);
            writer.Write(IdealRange);
            writer.Write(MaximumRange);
            writer.Write(PointBlankAccuracy);
            writer.Write(IdealAccuracy);
            writer.Write(MaximumRangeAccuracy);
        }
    }
}
