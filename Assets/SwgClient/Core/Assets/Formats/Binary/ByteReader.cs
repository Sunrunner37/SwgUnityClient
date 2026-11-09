using System;
using System.IO;
using System.Text;

namespace Swg.Client.Core.Assets.Formats.Binary
{
    public sealed class ByteReader
    {
        public readonly byte[] Bytes;
        public int Position;
        public int Remaining => Bytes.Length - Position;

        public ByteReader(byte[] bytes)
        {
            Bytes = bytes;
        }

        public byte[] Take(int count)
        {
            if (count < 0 || count > Remaining)
                throw new InvalidDataException("Truncated packet");
            var b = new byte[count];
            Buffer.BlockCopy(Bytes, Position, b, 0, count);
            Position += count;
            return b;
        }

        public void Skip(int count)
        {
            if (count < 0 || count > Remaining)
                throw new InvalidDataException("Truncated packet");
            Position += count;
        }

        public byte Byte()
        {
            if (Remaining < 1)
                throw new InvalidDataException("Truncated packet");
            return Bytes[Position++];
        }

        public ushort U16()
        {
            return (ushort)(Byte() | Byte() << 8);
        }

        public ushort Net16()
        {
            return (ushort)(Byte() << 8 | Byte());
        }

        public uint U32()
        {
            return (uint)(Byte() | Byte() << 8 | Byte() << 16 | Byte() << 24);
        }

        public uint Net32()
        {
            return (uint)(Byte() << 24 | Byte() << 16 | Byte() << 8 | Byte());
        }

        public ulong U64()
        {
            return U32() | (ulong)U32() << 32;
        }

        public float Float()
        {
            var v = BitConverter.ToSingle(Take(4), 0);
            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new InvalidDataException("Invalid coordinate");
            return v;
        }

        public string Ascii()
        {
            return Encoding.UTF8.GetString(Take(U16()));
        }

        public string Unicode()
        {
            uint n = U32();
            if (n > 16384)
                throw new InvalidDataException("Oversized string");
            return Encoding.Unicode.GetString(Take(checked((int)n * 2)));
        }

        public int Count()
        {
            uint n = U32();
            if (n > 4096)
                throw new InvalidDataException("Oversized server list");
            return (int)n;
        }

        public string CString()
        {
            int start = Position;
            while (Byte() != 0)
            {
            }

            return Encoding.UTF8.GetString(Bytes, start, Position - start - 1);
        }

        public static uint At(byte[] b, int p)
        {
            if (p < 0 || p > b.Length - 4)
                throw new InvalidDataException("Invalid offset");
            return (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);
        }
    }
}
