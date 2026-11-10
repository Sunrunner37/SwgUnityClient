using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/ship/ShipUpdateTransformCollisionMessage.h:13 (ShipUpdateTransformCollisionMessage).
    public readonly struct ShipUpdateTransformCollisionPacket : IPacket
    {
        public const uint Opcode = 0x763648d0;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly Quaternion Rotation;
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly uint MovementCounter;
        ShipUpdateTransformCollisionPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 8))
                throw new InvalidDataException("Unexpected ShipUpdateTransformCollisionPacket header");
            ObjectId = reader.U64();
            Rotation = PacketFieldCodec.Rotation(reader);
            Position = PacketFieldCodec.Position(reader);
            Velocity = PacketFieldCodec.Vector(reader);
            MovementCounter = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ShipUpdateTransformCollisionPacket ReadFrom(ByteReader reader) => new ShipUpdateTransformCollisionPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            PacketFieldCodec.Rotation(writer, Rotation);
            PacketFieldCodec.Position(writer, Position);
            PacketFieldCodec.Vector(writer, Velocity);
            writer.Write(MovementCounter);
        }
    }
}
