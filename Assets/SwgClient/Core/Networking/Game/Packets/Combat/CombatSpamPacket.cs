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
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong AttackerId;
        public readonly ulong DefenderId;
        public readonly ulong ItemId;
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
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            AttackerId = reader.U64();
            DefenderId = reader.U64();
            ItemId = reader.U64();
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
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(AttackerId);
            writer.Write(DefenderId);
            writer.Write(ItemId);
            writer.Write(Damage);
            StringId.WriteTo(writer);
            writer.Write(Color);
            PacketFieldCodec.Unicode(writer, Text);
        }
    }
}
