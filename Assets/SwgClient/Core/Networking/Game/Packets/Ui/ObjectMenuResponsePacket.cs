using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ObjectMenuResponse.h:17 (ObjectMenuResponse).
    public readonly struct ObjectMenuResponsePacket : IPacket
    {
        public const uint ControllerType = 0x147;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong TargetSwgId;
        public readonly ulong PlayerSwgId;
        public readonly RadialMenuEntry[] Items;
        public readonly byte Counter;
        public ObjectMenuResponsePacket(ulong playerSwgId, ulong targetSwgId, RadialMenuEntry[] items, byte counter)
        {
            Flags = 0x0b;
            ReceiverSwgId = playerSwgId;
            EnvelopeReserved = 0;
            PlayerSwgId = playerSwgId;
            TargetSwgId = targetSwgId;
            Items = items;
            Counter = counter;
        }

        ObjectMenuResponsePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected ObjectMenuResponsePacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            TargetSwgId = reader.U64();
            PlayerSwgId = reader.U64();
            Items = new RadialMenuEntry[PacketFieldCodec.Count(reader, reader.U32(), 8)];
            for (int i = 0; i < Items.Length; i++)
                Items[i] = RadialMenuEntry.ReadFrom(reader);
            Counter = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static ObjectMenuResponsePacket ReadFrom(ObjectControllerPacket packet) => new ObjectMenuResponsePacket(packet);
        public static ObjectMenuResponsePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(TargetSwgId);
            writer.Write(PlayerSwgId);
            writer.Write(checked((uint)Items.Length));
            foreach (var item in Items)
                item.WriteTo(writer);
            writer.Write(Counter);
        }
    }
}
