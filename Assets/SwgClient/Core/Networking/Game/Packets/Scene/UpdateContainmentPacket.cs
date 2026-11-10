using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct UpdateContainmentPacket : IPacket
    {
        public const ushort OperandCount = 4;
        public const uint Opcode = 0x56cbde9e;
        public readonly ulong ObjectId;
        public readonly ulong ParentId;
        public readonly uint Arrangement;
        public UpdateContainmentPacket(ulong objectId, ulong parentId, uint arrangement)
        {
            ObjectId = objectId;
            ParentId = parentId;
            Arrangement = arrangement;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            writer.Write(ParentId);
            writer.Write(Arrangement);
        }

        public static UpdateContainmentPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected UpdateContainmentPacket header");
            return new UpdateContainmentPacket(reader.U64(), reader.U64(), reader.U32());
        }
    }
}
