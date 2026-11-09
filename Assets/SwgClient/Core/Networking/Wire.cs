using System;
using System.IO;
using System.Text;

namespace Swg.Client.Core.Networking
{
    // Binary primitives only. Packet types own opcodes, headers and field order.
    internal static class Wire
    {
        public const int HeaderSize = sizeof(ushort) + sizeof(uint);
        public static void ValidateAscii(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > ushort.MaxValue)
                throw new InvalidDataException("String too long");
            foreach (char c in value)
                if (c > 127)
                    throw new InvalidDataException("Login fields must contain ASCII characters");
        }

        public static void Ascii(BinaryWriter writer, string value)
        {
            ValidateAscii(value);
            writer.Write((ushort)value.Length);
            writer.Write(Encoding.ASCII.GetBytes(value));
        }

        public static void Unicode(BinaryWriter writer, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > 16384)
                throw new InvalidDataException("Oversized string");
            writer.Write((uint)value.Length);
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        public static void Count(BinaryWriter writer, int count)
        {
            if (count < 0 || count > 4096)
                throw new InvalidDataException("Oversized server list");
            writer.Write((uint)count);
        }
    }
}
