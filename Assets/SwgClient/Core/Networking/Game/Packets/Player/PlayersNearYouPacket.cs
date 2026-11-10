using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PlayersNearYou.h:13 (PlayersNearYouMessage).
    public readonly struct PlayersNearYouPacket : IPacket
    {
        public const uint ControllerType = 0x1e7;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly NearbyPlayer[] Players;
        PlayersNearYouPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected PlayersNearYouPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Players = new NearbyPlayer[PacketFieldCodec.Count(reader, reader.U32(), 20)];
            for (int i = 0; i < Players.Length; i++)
                Players[i] = NearbyPlayer.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static PlayersNearYouPacket ReadFrom(ObjectControllerPacket packet) => new PlayersNearYouPacket(packet);
        public static PlayersNearYouPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(checked((uint)Players.Length));
            foreach (var item in Players)
                item.WriteTo(writer);
        }
    }
}
