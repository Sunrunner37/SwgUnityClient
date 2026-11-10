using System.IO;
using System.Text;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    internal static class PacketFieldCodec
    {
        public static int Count(ByteReader reader, uint count, int minimumElementBytes)
        {
            if (count > int.MaxValue || (minimumElementBytes > 0 && count > reader.Remaining / minimumElementBytes))
                throw new InvalidDataException("Truncated packet collection");
            return (int)count;
        }

        public static void End(ByteReader reader)
        {
            if (reader.Remaining != 0)
                throw new InvalidDataException("Unexpected trailing packet fields");
        }

        public static void Ascii(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        public static void Unicode(BinaryWriter writer, string value)
        {
            writer.Write(checked((uint)value.Length));
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        public static Vector3 Position(ByteReader reader)
        {
            float x = reader.Float();
            float z = reader.Float();
            float y = reader.Float();
            return new Vector3(x, y, z);
        }

        public static Vector3 Vector(ByteReader reader) => new Vector3(reader.Float(), reader.Float(), reader.Float());
        public static Quaternion Rotation(ByteReader reader) => new Quaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float());
        public static void Position(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.z);
            writer.Write(value.y);
        }

        public static void Vector(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public static void Rotation(BinaryWriter writer, Quaternion value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }
    }
}
