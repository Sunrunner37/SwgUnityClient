using System.IO;
using Swg.Client.Core;
namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    /// <summary>Core3 ItemDropTradeCallback (0x115): request a player trade without dropping an item.</summary>
    public readonly struct TradeRequestPacket : IPacket
    {
        public const uint ControllerType = 0x115;
        public const uint Flags = 0x23;
        public readonly SwgId PlayerSwgId;
        public readonly SwgId TargetSwgId;
        public TradeRequestPacket(SwgId playerSwgId, SwgId targetSwgId) { PlayerSwgId = playerSwgId; TargetSwgId = targetSwgId; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags); writer.Write(ControllerType); writer.Write(PlayerSwgId.Value); writer.Write(0u);
            // Core3 skips the two unused drag/item identifiers before reading the trade target.
            writer.Write(0UL); writer.Write(0UL); writer.Write(TargetSwgId.Value);
        }
    }
}
