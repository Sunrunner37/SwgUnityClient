using System.IO;

namespace Swg.Client.Core.Networking.Soe
{
    internal static class NetworkByteOrder
    {
        public static void Write16(BinaryWriter writer, ushort value)
        {
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        public static void Write32(BinaryWriter writer, uint value)
        {
            writer.Write((byte)(value >> 24));
            writer.Write((byte)(value >> 16));
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }
    }
}
