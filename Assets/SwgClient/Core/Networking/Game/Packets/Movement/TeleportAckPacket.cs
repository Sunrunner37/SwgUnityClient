using System.IO;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    /// <summary>Core3 TeleportAckCallback (0x13F) releases the server's movement lock after an authoritative player transform.</summary>
    public readonly struct TeleportAckPacket : IPacket
    {
        public const uint ControllerType = 0x13f;
        public readonly SwgId PlayerSwgId;
        public readonly uint MovementCounter;

        public TeleportAckPacket(SwgId playerSwgId, uint movementCounter)
        {
            PlayerSwgId = playerSwgId;
            MovementCounter = movementCounter;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(ControllerType);
            writer.Write(PlayerSwgId.Value);
            writer.Write(0u);
            writer.Write(MovementCounter);
        }
    }
}
