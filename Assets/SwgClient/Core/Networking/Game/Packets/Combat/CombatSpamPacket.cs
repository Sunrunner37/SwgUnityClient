using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatSpam.h:12 (CombatSpam).
    public readonly struct CombatSpamPacket : IPacket
    {
        public const uint ControllerType = 0x134;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong AttackerSwgId;
        public readonly ulong DefenderSwgId;
        public readonly ulong ItemSwgId;
        public readonly uint Damage;
        public readonly StringIdReference StringId;
        public readonly byte Color;
        public readonly string Text;
        CombatSpamPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CombatSpamPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            AttackerSwgId = reader.U64();
            DefenderSwgId = reader.U64();
            ItemSwgId = reader.U64();
            Damage = reader.U32();
            StringId = StringIdReference.ReadFrom(reader);
            Color = reader.Byte();
            Text = reader.Unicode();
            PacketFieldCodec.End(reader);
        }

        public static CombatSpamPacket ReadFrom(ObjectControllerPacket packet) => new CombatSpamPacket(packet);
        public static CombatSpamPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(AttackerSwgId);
            writer.Write(DefenderSwgId);
            writer.Write(ItemSwgId);
            writer.Write(Damage);
            StringId.WriteTo(writer);
            writer.Write(Color);
            PacketFieldCodec.Unicode(writer, Text);
        }
    }
}
