using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3 ObjectMenuRequestCallback. The controller reserved word is its legacy size field.
    public readonly struct ObjectMenuRequestPacket : IPacket
    {
        public const uint ControllerType = 0x146;
        public readonly SwgId PlayerSwgId;
        public readonly SwgId TargetSwgId;
        public readonly RadialMenuEntry[] Items;
        public readonly byte Counter;

        public ObjectMenuRequestPacket(SwgId playerSwgId, SwgId targetSwgId, RadialMenuEntry[] items, byte counter)
        {
            PlayerSwgId = playerSwgId;
            TargetSwgId = targetSwgId;
            Items = items;
            Counter = counter;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(ControllerType);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u);
            writer.Write(TargetSwgId.Value);
            writer.Write(PlayerSwgId.Value);
            writer.Write((uint)Items.Length);
            foreach (var item in Items)
                item.WriteTo(writer);
            writer.Write(Counter);
        }

        public static ObjectMenuRequestPacket ReadFrom(ByteReader reader)
        {
            var envelope = ObjectControllerPacket.ReadFrom(reader);
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected object menu request controller type");
            reader = new ByteReader(envelope.Payload);
            SwgId target = reader.U64();
            SwgId player = reader.U64();
            var items = new RadialMenuEntry[reader.Count()];
            for (int i = 0; i < items.Length; i++)
                items[i] = RadialMenuEntry.ReadFrom(reader);
            byte counter = reader.Byte();
            PacketFieldCodec.End(reader);
            return new ObjectMenuRequestPacket(player, target, items, counter);
        }
    }
}
