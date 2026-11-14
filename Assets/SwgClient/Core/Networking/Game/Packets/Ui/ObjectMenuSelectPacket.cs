using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Assets.Formats.Binary;
namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    /// <summary>Core3 ObjectMenuSelectCallback: object ID followed by its radial action ID.</summary>
    public readonly struct ObjectMenuSelectPacket : IPacket
    {
        public const ushort OperandCount = 3;
        public const uint Opcode = 0x7ca18726;
        public readonly SwgId TargetSwgId;
        public readonly byte RadialId;
        public ObjectMenuSelectPacket(SwgId targetSwgId, byte radialId) { TargetSwgId = targetSwgId; RadialId = radialId; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(TargetSwgId.Value);
            writer.Write(RadialId);
        }
        public static ObjectMenuSelectPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode) throw new InvalidDataException("Unexpected object menu selection header");
            var packet = new ObjectMenuSelectPacket(reader.U64(), reader.Byte());
            PacketFieldCodec.End(reader);
            return packet;
        }
    }
}
