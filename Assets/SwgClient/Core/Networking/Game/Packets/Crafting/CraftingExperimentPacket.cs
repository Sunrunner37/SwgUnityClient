using System.Collections.Generic;
using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Crafting
{
    /// <summary>Core3 CraftingExperimentCallback (0x106): rows and points allocated to each row.</summary>
    public readonly struct CraftingExperimentPacket : IPacket
    {
        public readonly SwgId PlayerSwgId;
        public readonly byte Counter;
        public readonly IReadOnlyList<(int Row, int Points)> Attempts;
        public CraftingExperimentPacket(SwgId playerSwgId, byte counter, IReadOnlyList<(int Row, int Points)> attempts)
        { PlayerSwgId = playerSwgId; Counter = counter; Attempts = attempts; }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(0x106u);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u);
            writer.Write(Counter);
            writer.Write(Attempts.Count);
            foreach (var attempt in Attempts) { writer.Write(attempt.Row); writer.Write(attempt.Points); }
        }
    }
}
