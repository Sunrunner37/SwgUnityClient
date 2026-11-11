using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatAction.h.
    public readonly struct CombatActionPacket : IPacket
    {
        public const uint ControllerType = 0xcc;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint AnimationCrc;
        public readonly ulong AttackerSwgId;
        public readonly ulong WeaponSwgId;
        public readonly byte AttackerPosture;
        public readonly byte Trails;
        public readonly byte Reserved;
        public readonly bool ExtendedDefenders;
        public readonly CombatDefender[] Defenders;
        CombatActionPacket(ObjectControllerPacket packet)
        {
            this = default;
            if (packet.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CombatActionPacket controller type");
            Flags = packet.Flags;
            ReceiverSwgId = packet.SwgId;
            EnvelopeReserved = packet.Reserved;
            var reader = new ByteReader(packet.Payload);
            AnimationCrc = reader.U32();
            AttackerSwgId = reader.U64();
            WeaponSwgId = reader.U64();
            AttackerPosture = reader.Byte();
            Trails = reader.Byte();
            Reserved = reader.Byte();
            int count = reader.Remaining >= 2 ? reader.Bytes[reader.Position] | reader.Bytes[reader.Position + 1] << 8 : 0;
            // Legacy constructors use one ushort count followed by 11-byte defenders.
            // Core3's hit-list constructor writes a ushort index on EACH 15-byte defender instead.
            ExtendedDefenders = reader.Remaining != 2 + count * 11;
            if (ExtendedDefenders)
            {
                if (reader.Remaining % 15 != 0)
                    throw new InvalidDataException("Invalid extended combat defender list");
                count = reader.Remaining / 15;
            }
            else
                reader.U16();
            Defenders = new CombatDefender[count];
            for (int i = 0; i < count; i++)
                Defenders[i] = new CombatDefender(reader, ExtendedDefenders);
            PacketFieldCodec.End(reader);
        }

        public static CombatActionPacket ReadFrom(ObjectControllerPacket packet) => new CombatActionPacket(packet);
        public static CombatActionPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(AnimationCrc);
            writer.Write(AttackerSwgId);
            writer.Write(WeaponSwgId);
            writer.Write(AttackerPosture);
            writer.Write(Trails);
            writer.Write(Reserved);
            if (!ExtendedDefenders)
                writer.Write(checked((ushort)Defenders.Length));
            foreach (var defender in Defenders)
                defender.WriteTo(writer, ExtendedDefenders);
        }
    }
}
