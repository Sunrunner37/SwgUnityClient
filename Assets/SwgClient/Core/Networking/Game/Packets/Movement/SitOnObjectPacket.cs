using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/SitOnObject.h:10 (SitOnObject).
    public readonly struct SitOnObjectPacket : IPacket
    {
        public const uint ControllerType = 0x13b;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong ParentSwgId;
        public readonly Vector3 Position;
        SitOnObjectPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected SitOnObjectPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ParentSwgId = reader.U64();
            Position = PacketFieldCodec.Vector(reader);
            PacketFieldCodec.End(reader);
        }

        public static SitOnObjectPacket ReadFrom(ObjectControllerPacket packet) => new SitOnObjectPacket(packet);
        public static SitOnObjectPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(ParentSwgId);
            PacketFieldCodec.Vector(writer, Position);
        }
    }
}
