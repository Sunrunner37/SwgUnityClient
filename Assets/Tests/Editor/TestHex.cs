using System;

namespace Swg.Client.Tests.Editor
{
    internal static class TestHex
    {
        internal static byte[] Decode(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}
