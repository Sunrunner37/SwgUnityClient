using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using UnityEngine;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    /// <summary>Client movement request (ObjectController CM_netUpdateTransform/WithParent), not the server's compressed transform broadcast.</summary>
    public readonly struct DataTransformPacket : IPacket
    {
        public const ushort OperandCount = 5;
        public const uint Opcode = 0x80ce5e46;
        public const uint Flags = 0x23;
        public const uint WorldTransform = 0x71;
        public const uint ParentTransform = 0xf1;
        public readonly SwgId SwgId;
        public readonly SwgId ParentSwgId;
        public readonly uint Timestamp;
        public readonly uint MovementCounter;
        public readonly Vector3 ServerPosition;
        public readonly Quaternion Rotation;
        public readonly float Speed;
        public DataTransformPacket(SwgId swgId, SwgId parentSwgId, uint timestamp, uint movementCounter, Vector3 serverPosition, Quaternion rotation, float speed)
        {
            SwgId = swgId;
            ParentSwgId = parentSwgId;
            Timestamp = timestamp;
            MovementCounter = movementCounter;
            ServerPosition = serverPosition;
            Rotation = rotation;
            Speed = speed;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Flags);
            writer.Write(ParentSwgId.IsNone ? WorldTransform : ParentTransform);
            writer.Write((ulong)SwgId);
            // Client-to-server movement uses this slot as its timestamp. The server-to-client envelope has a reserved zero here.
            writer.Write(Timestamp);
            writer.Write(MovementCounter);
            if (!ParentSwgId.IsNone)
                writer.Write((ulong)ParentSwgId);
            writer.Write(Rotation.x);
            writer.Write(Rotation.y);
            writer.Write(Rotation.z);
            writer.Write(Rotation.w);
            // SWG wire order is X, vertical Z, horizontal Y; our vectors use Unity's X/Y/Z axis convention.
            writer.Write(ServerPosition.x);
            writer.Write(ServerPosition.y);
            writer.Write(ServerPosition.z);
            writer.Write(Speed);
        }

        public static DataTransformPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode || reader.U32() != Flags)
                throw new InvalidDataException("Unexpected client DataTransform header");
            uint type = reader.U32();
            if (type != WorldTransform && type != ParentTransform)
                throw new InvalidDataException("Unexpected movement controller type");
            SwgId swgId = reader.U64();
            uint stamp = reader.U32();
            uint counter = reader.U32();
            SwgId parent = type == ParentTransform ? reader.U64() : 0;
            var rotation = new Quaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float());
            var position = new Vector3(reader.Float(), reader.Float(), reader.Float());
            return new DataTransformPacket(swgId, parent, stamp, counter, position, rotation, reader.Float());
        }
    }
}
