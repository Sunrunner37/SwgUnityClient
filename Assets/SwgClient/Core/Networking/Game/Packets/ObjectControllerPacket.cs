using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets
{
    public readonly struct ObjectControllerPacket : IPacket
    {
        public const ushort OperandCount = 5;
        public const uint Opcode = 0x80ce5e46;
        public readonly uint Flags;
        public readonly uint ControllerType;
        public readonly ulong ObjectId;
        public readonly uint Reserved;
        public readonly byte[] Payload;
        public ObjectControllerPacket(uint flags, uint controllerType, ulong objectId, byte[] payload, uint reserved = 0)
        {
            Flags = flags;
            ControllerType = controllerType;
            ObjectId = objectId;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Reserved = reserved;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ObjectId);
            writer.Write(Reserved);
            writer.Write(Payload);
        }

        public static ObjectControllerPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected ObjectControllerPacket header");
            uint flags = reader.U32();
            uint type = reader.U32();
            ulong id = reader.U64();
            uint reserved = reader.U32();
            if (reader.Remaining > 1500000)
                throw new InvalidDataException("Oversized object-controller payload");
            return new ObjectControllerPacket(flags, type, id, reader.Take(reader.Remaining), reserved);
        }
    }
}
