using UnityEngine;
using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    public readonly struct UpdateTransformWithParentPacket : IPacket
    {
        public const ushort OperandCount = 8;
        public const uint Opcode = 0xc867ab5a;
        public readonly ulong ObjectId;
        public readonly Vector3 Position;
        public readonly uint MovementCounter;
        public readonly byte Speed;
        public readonly byte Direction;
        public readonly ulong ParentId;
        public UpdateTransformWithParentPacket(ulong objectId, Vector3 position, uint movementCounter, byte speed, byte direction, ulong parentId)
        {
            ObjectId = objectId;
            Position = position;
            MovementCounter = movementCounter;
            Speed = speed;
            Direction = direction;
            ParentId = parentId;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ParentId);
            writer.Write(ObjectId);
            writer.Write(checked((short)(Position.x * 8)));
            writer.Write(checked((short)(Position.y * 8)));
            writer.Write(checked((short)(Position.z * 8)));
            writer.Write(MovementCounter);
            writer.Write(Speed);
            writer.Write(Direction);
        }

        public static UpdateTransformWithParentPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected UpdateTransformWithParentPacket header");
            ulong parent = reader.U64();
            ulong id = reader.U64();
            var position = new Vector3(unchecked((short)reader.U16()) / 8f, unchecked((short)reader.U16()) / 8f, unchecked((short)reader.U16()) / 8f);
            return new UpdateTransformWithParentPacket(id, position, reader.U32(), reader.Byte(), reader.Byte(), parent);
        }
    }
}
