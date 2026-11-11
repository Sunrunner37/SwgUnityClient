using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Replication
{
    public readonly struct DeltasPacket : IPacket
    {
        public const ushort OperandCount = 5;
        public const uint Opcode = 0x12862153;
        public readonly ulong SwgId;
        public readonly uint ObjectType;
        public readonly byte View;
        public readonly byte[] Payload;
        public DeltasPacket(ulong swgId, uint objectType, byte view, byte[] payload)
        {
            SwgId = swgId;
            ObjectType = objectType;
            View = view;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(ObjectType);
            writer.Write(View);
            writer.Write((uint)Payload.Length);
            writer.Write(Payload);
        }

        public static DeltasPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected DeltasPacket header");
            ulong swgId = reader.U64();
            uint type = reader.U32();
            byte view = reader.Byte();
            int length = checked((int)reader.U32());
            if (length < 2 || length > 1500000 || length != reader.Remaining)
                throw new InvalidDataException("Invalid baseline/delta payload length");
            return new DeltasPacket(swgId, type, view, reader.Take(length));
        }
    }
}
