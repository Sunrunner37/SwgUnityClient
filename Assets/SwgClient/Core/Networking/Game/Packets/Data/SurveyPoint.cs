using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct SurveyPoint
    {
        public readonly Vector3 Position;
        public readonly float Concentration;
        SurveyPoint(ByteReader reader)
        {
            this = default;
            Position = PacketFieldCodec.Position(reader);
            Concentration = reader.Float();
        }

        public static SurveyPoint ReadFrom(ByteReader reader) => new SurveyPoint(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Position(writer, Position);
            writer.Write(Concentration);
        }
    }
}
