using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    /// <summary>Core3 CraftingRemoveIngredientCallback (0x108); returns the entire slot to the inventory.</summary>
    public readonly struct CraftingRemoveIngredientPacket : IPacket
    {
        public readonly SwgId PlayerSwgId;
        public readonly SwgId InventorySwgId;
        public readonly int Slot;
        public readonly byte Counter;
        public CraftingRemoveIngredientPacket(SwgId playerSwgId, SwgId inventorySwgId, int slot, byte counter)
        { PlayerSwgId = playerSwgId; InventorySwgId = inventorySwgId; Slot = slot; Counter = counter; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(0x108u);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u); // Consumed by the callback before the slot index.
            writer.Write(Slot);
            writer.Write(InventorySwgId.Value);
            writer.Write(Counter);
        }
    }
}
