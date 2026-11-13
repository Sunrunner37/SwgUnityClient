using System.IO;

namespace Swg.Client.Core.Networking.Game.Packets.Targeting
{
    // Core3 TargetUpdateCallback: update the player's selected object, including zero to clear.
    public readonly struct TargetUpdatePacket : IPacket
    {
        public const uint ControllerType = 0x126;
        public readonly SwgId PlayerSwgId;
        public readonly SwgId TargetSwgId;

        public TargetUpdatePacket(SwgId playerSwgId, SwgId targetSwgId)
        {
            PlayerSwgId = playerSwgId;
            TargetSwgId = targetSwgId;
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
        }
    }
}
