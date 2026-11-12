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

        public static void SkipVectorDelta(ByteReader reader, int elementSize)
        {
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                if (operation == 4)
                    continue;
                reader.U16();
                if (operation == 1 || operation == 2)
                    reader.Skip(elementSize);
                else if (operation != 0)
                    throw new InvalidDataException("Unsupported creature vector operation " + operation);
            }
        }
    }
}
