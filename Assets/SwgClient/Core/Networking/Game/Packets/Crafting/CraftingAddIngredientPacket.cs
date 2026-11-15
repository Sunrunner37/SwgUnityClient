using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    /// <summary>Core3 CraftingAddIngredientCallback; the server chooses the quantity still needed.</summary>
    public readonly struct CraftingAddIngredientPacket : IPacket
    {
        public readonly SwgId PlayerSwgId;
        public readonly SwgId IngredientSwgId;
        public readonly int Slot;
        public readonly byte Counter;
        public CraftingAddIngredientPacket(SwgId playerSwgId, SwgId ingredientSwgId, int slot, byte counter)
        { PlayerSwgId = playerSwgId; IngredientSwgId = ingredientSwgId; Slot = slot; Counter = counter; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(0x107u);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u);
            writer.Write(IngredientSwgId.Value);
            writer.Write(Slot);
            writer.Write(0u);
            writer.Write(Counter);
        }
    }
}
