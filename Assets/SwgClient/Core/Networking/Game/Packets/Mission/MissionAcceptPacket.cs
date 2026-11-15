using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    /// <summary>Core3 MissionAcceptCallback; callback consumes the envelope's reserved word.</summary>
    public readonly struct MissionAcceptPacket : IPacket
    {
        public readonly SwgId MissionSwgId;
        public readonly SwgId TerminalSwgId;
        public MissionAcceptPacket(SwgId playerSwgId, SwgId missionSwgId, SwgId terminalSwgId) { PlayerSwgId = playerSwgId; MissionSwgId = missionSwgId; TerminalSwgId = terminalSwgId; }
        public readonly SwgId PlayerSwgId;
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u); writer.Write(0xf9u); writer.Write(PlayerSwgId.Value); writer.Write(0u);
            writer.Write(MissionSwgId.Value); writer.Write(TerminalSwgId.Value); writer.Write((byte)0);
        }
    }
}
