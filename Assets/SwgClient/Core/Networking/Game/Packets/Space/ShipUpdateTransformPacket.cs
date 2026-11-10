using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/ship/ShipUpdateTransformMessage.h:16 (ShipUpdateTransformMessage).
    public readonly struct ShipUpdateTransformPacket : IPacket
    {
        public const uint Opcode = 0x76026fb9;
        public readonly ushort OperandCount;
        public readonly ushort UniqueId;
        public readonly sbyte[] PackedRotation;
        public readonly short[] PackedPosition;
        public readonly short PackedSpeed;
        public readonly ushort PackedDirection;
        public readonly sbyte[] PackedRotationRate;
        public readonly uint MovementCounter;
        ShipUpdateTransformPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 8))
                throw new InvalidDataException("Unexpected ShipUpdateTransformPacket header");
            UniqueId = reader.U16();
            PackedRotation = new sbyte[4];
            for (int i = 0; i < PackedRotation.Length; i++)
                PackedRotation[i] = unchecked((sbyte)reader.Byte());
            PackedPosition = new short[3];
            for (int i = 0; i < PackedPosition.Length; i++)
                PackedPosition[i] = unchecked((short)reader.U16());
            PackedSpeed = unchecked((short)reader.U16());
            PackedDirection = reader.U16();
            PackedRotationRate = new sbyte[3];
            for (int i = 0; i < PackedRotationRate.Length; i++)
                PackedRotationRate[i] = unchecked((sbyte)reader.Byte());
            MovementCounter = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ShipUpdateTransformPacket ReadFrom(ByteReader reader) => new ShipUpdateTransformPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(UniqueId);
            foreach (var item in PackedRotation)
                writer.Write(item);
            foreach (var item in PackedPosition)
                writer.Write(item);
            writer.Write(PackedSpeed);
            writer.Write(PackedDirection);
            foreach (var item in PackedRotationRate)
                writer.Write(item);
            writer.Write(MovementCounter);
        }
    }
}
