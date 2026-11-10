using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class ReplicationReader
    {
        public static void RequireEnd(ByteReader reader)
        {
            if (reader.Remaining != 0)
                throw new InvalidDataException("Unexpected trailing replication payload");
        }

        public static byte[] Customization(ByteReader reader) => reader.Take(reader.U16());
        public static void SkipVector(ByteReader reader, int elementBytes)
        {
            int count = reader.Count();
            reader.U32(); // Delta-vector update counter.
            reader.Skip(checked(count * elementBytes));
        }
    }
}
