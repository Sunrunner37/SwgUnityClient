using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/CharacterSheetResponseMessage.h:11 (CharacterSheetResponseMessage).
    public readonly struct CharacterSheetResponsePacket : IPacket
    {
        public const uint Opcode = 0x9b3a17c4;
        public readonly ushort OperandCount;
        public readonly uint Reserved1;
        public readonly uint Reserved2;
        public readonly Vector3 CloningPosition;
        public readonly string CloningPlanet;
        public readonly Vector3 BankPosition;
        public readonly string BankPlanet;
        public readonly Vector3 ResidencePosition;
        public readonly string ResidencePlanet;
        public readonly string SpouseName;
        public readonly int LotsRemaining;
        public readonly uint Faction;
        public readonly uint FactionStatus;
        CharacterSheetResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 13))
                throw new InvalidDataException("Unexpected CharacterSheetResponsePacket header");
            Reserved1 = reader.U32();
            Reserved2 = reader.U32();
            CloningPosition = PacketFieldCodec.Position(reader);
            CloningPlanet = reader.Ascii();
            BankPosition = PacketFieldCodec.Position(reader);
            BankPlanet = reader.Ascii();
            ResidencePosition = PacketFieldCodec.Position(reader);
            ResidencePlanet = reader.Ascii();
            SpouseName = reader.Unicode();
            LotsRemaining = unchecked((int)reader.U32());
            Faction = reader.U32();
            FactionStatus = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CharacterSheetResponsePacket ReadFrom(ByteReader reader) => new CharacterSheetResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved1);
            writer.Write(Reserved2);
            PacketFieldCodec.Position(writer, CloningPosition);
            PacketFieldCodec.Ascii(writer, CloningPlanet);
            PacketFieldCodec.Position(writer, BankPosition);
            PacketFieldCodec.Ascii(writer, BankPlanet);
            PacketFieldCodec.Position(writer, ResidencePosition);
            PacketFieldCodec.Ascii(writer, ResidencePlanet);
            PacketFieldCodec.Unicode(writer, SpouseName);
            writer.Write(LotsRemaining);
            writer.Write(Faction);
            writer.Write(FactionStatus);
        }
    }
}
