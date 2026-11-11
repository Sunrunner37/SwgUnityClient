using UnityEngine;
using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    public readonly struct UpdateTransformPacket : IPacket
    {
        public const ushort OperandCount = 8;
        public const uint Opcode = 0x1b24f808;
        public readonly ulong SwgId;
        public readonly Vector3 Position;
        public readonly uint MovementCounter;
        public readonly byte Speed;
        public readonly byte Direction;
        public UpdateTransformPacket(ulong swgId, Vector3 position, uint movementCounter, byte speed, byte direction)
        {
            SwgId = swgId;
            Position = position;
            MovementCounter = movementCounter;
            Speed = speed;
            Direction = direction;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(checked((short)(Position.x * 4)));
            writer.Write(checked((short)(Position.y * 4)));
            writer.Write(checked((short)(Position.z * 4)));
            writer.Write(MovementCounter);
            writer.Write(Speed);
            writer.Write(Direction);
        }

        public static UpdateTransformPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected UpdateTransformPacket header");
            ulong swgId = reader.U64();
            var position = new Vector3(unchecked((short)reader.U16()) / 4f, unchecked((short)reader.U16()) / 4f, unchecked((short)reader.U16()) / 4f);
            return new UpdateTransformPacket(swgId, position, reader.U32(), reader.Byte(), reader.Byte());
        }
    }
}
