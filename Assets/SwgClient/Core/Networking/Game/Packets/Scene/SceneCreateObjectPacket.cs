using UnityEngine;
using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct SceneCreateObjectPacket : IPacket
    {
        public const ushort OperandCount = 5;
        public const uint Opcode = 0xfe89ddea;
        public readonly ulong ObjectId;
        public readonly Quaternion Rotation;
        public readonly Vector3 Position;
        public readonly uint TemplateCrc;
        public readonly byte Flags;
        public SceneCreateObjectPacket(ulong objectId, Quaternion rotation, Vector3 position, uint templateCrc, byte flags = 0)
        {
            ObjectId = objectId;
            Rotation = rotation;
            Position = position;
            TemplateCrc = templateCrc;
            Flags = flags;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            writer.Write(Rotation.x);
            writer.Write(Rotation.y);
            writer.Write(Rotation.z);
            writer.Write(Rotation.w);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(TemplateCrc);
            writer.Write(Flags);
        }

        public static SceneCreateObjectPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected SceneCreateObjectPacket header");
            ulong id = reader.U64();
            var rotation = new Quaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float());
            var position = new Vector3(reader.Float(), reader.Float(), reader.Float());
            return new SceneCreateObjectPacket(id, rotation, position, reader.U32(), reader.Byte());
        }
    }
}
