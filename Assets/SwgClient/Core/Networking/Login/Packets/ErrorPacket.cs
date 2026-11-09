using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct ErrorPacket : IPacket
    {
        public const ushort OperandCount = 3;
        public const uint Opcode = 0xb5abf91a;
        public readonly string Title;
        public readonly string Text;
        public readonly byte Fatal;
        public ErrorPacket(string title, string text, byte fatal = 0)
        {
            Title = title;
            Text = text;
            Fatal = fatal;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Wire.Ascii(writer, Title);
            Wire.Ascii(writer, Text);
            writer.Write(Fatal);
        }

        public static ErrorPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            return new ErrorPacket(reader.Ascii(), reader.Ascii(), reader.Byte());
        }

        public Exception ToException() => new InvalidOperationException(Title + ": " + Text);
    }
}
