using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct MfdMember
    {
        public readonly string Planet;
        public readonly ulong ObjectId;
        public readonly Vector3 Position;
        MfdMember(ByteReader reader)
        {
            this = default;
            Planet = reader.Ascii();
            ObjectId = reader.U64();
            Position = PacketFieldCodec.Position(reader);
        }

        public static MfdMember ReadFrom(ByteReader reader) => new MfdMember(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Ascii(writer, Planet);
            writer.Write(ObjectId);
            PacketFieldCodec.Position(writer, Position);
        }
    }
}
