using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;
namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    /// <summary>Core3 CraftingCustomizationCallback: item name, template choice and manufacturing limit.</summary>
    public readonly struct CraftingCustomizationPacket : IPacket
    {
        public readonly SwgId PlayerSwgId;
        public readonly string Name;
        public readonly int ManufacturingRuns;
        public CraftingCustomizationPacket(SwgId playerSwgId, string name, int manufacturingRuns)
        { PlayerSwgId = playerSwgId; Name = name; ManufacturingRuns = manufacturingRuns; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(0x15au);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u);
            PacketFieldCodec.Unicode(writer, Name);
            writer.Write((byte)0xff); // Preserve the prototype template; no alternate template selected.
            writer.Write(ManufacturingRuns);
            writer.Write((byte)0); // No palette changes.
        }
    }
}
