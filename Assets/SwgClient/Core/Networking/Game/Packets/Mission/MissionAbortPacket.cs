using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    /// <summary>Core3 MissionAbortCallback; callback consumes the envelope's reserved word.</summary>
    public readonly struct MissionAbortPacket : IPacket
    {
        public readonly SwgId MissionSwgId;
        public MissionAbortPacket(SwgId playerSwgId, SwgId missionSwgId) { PlayerSwgId = playerSwgId; MissionSwgId = missionSwgId; }
        public readonly SwgId PlayerSwgId;
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u); writer.Write(0x142u); writer.Write(PlayerSwgId.Value); writer.Write(0u);
            writer.Write(MissionSwgId.Value);
        }
    }
}
