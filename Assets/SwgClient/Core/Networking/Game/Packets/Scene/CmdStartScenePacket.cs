using UnityEngine;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct CmdStartScenePacket : IPacket
    {
        public const ushort OperandCount = 9;
        public const uint Opcode = 0x3ae6dfae;
        public readonly byte Flags;
        public readonly ulong PlayerId;
        public readonly string TerrainPath;
        public readonly Vector3 Position;
        public readonly string TemplatePath;
        public readonly ulong GalacticTime;
        public CmdStartScenePacket(ulong playerId, string terrainPath, Vector3 position, string templatePath, ulong galacticTime, byte flags = 0)
        {
            PlayerId = playerId;
            TerrainPath = terrainPath;
            Position = position;
            TemplatePath = templatePath;
            GalacticTime = galacticTime;
            Flags = flags;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Flags);
            writer.Write(PlayerId);
            Wire.Ascii(writer, TerrainPath);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            Wire.Ascii(writer, TemplatePath);
            writer.Write(GalacticTime);
        }

        public static CmdStartScenePacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected CmdStartScenePacket header");
            byte flags = reader.Byte();
            ulong id = reader.U64();
            string terrain = reader.Ascii();
            var position = new Vector3(reader.Float(), reader.Float(), reader.Float());
            return new CmdStartScenePacket(id, terrain, position, reader.Ascii(), reader.U64(), flags);
        }
    }
}
