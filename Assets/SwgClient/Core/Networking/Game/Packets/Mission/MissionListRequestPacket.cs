using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    /// <summary>Core3 MissionListRequestCallback; callback consumes the envelope's reserved word.</summary>
    public readonly struct MissionListRequestPacket : IPacket
    {
        public readonly SwgId TerminalSwgId;
        public readonly byte Sequence;
        public MissionListRequestPacket(SwgId playerSwgId, SwgId terminalSwgId, byte sequence) { PlayerSwgId = playerSwgId; TerminalSwgId = terminalSwgId; Sequence = sequence; }
        public readonly SwgId PlayerSwgId;
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u); writer.Write(0xf5u); writer.Write(PlayerSwgId.Value); writer.Write(0u);
            writer.Write((byte)0); writer.Write(Sequence); writer.Write(TerminalSwgId.Value);
        }
    }
}
