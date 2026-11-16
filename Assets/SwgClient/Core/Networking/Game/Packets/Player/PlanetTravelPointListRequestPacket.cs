using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    /// <summary>Core3 PlanetTravelPointListRequestCallback reads the player ID followed by the zone name.</summary>
    public readonly struct PlanetTravelPointListRequestPacket : IPacket
    {
        public const uint Opcode = 0x96405d4d;
        public readonly SwgId PlayerSwgId;
        public readonly string Planet;
        public PlanetTravelPointListRequestPacket(SwgId playerSwgId, string planet)
        {
            PlayerSwgId = playerSwgId;
            Planet = planet;
        }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)3);
            writer.Write(Opcode);
            writer.Write(PlayerSwgId.Value);
            PacketFieldCodec.Ascii(writer, Planet);
        }
    }
}
