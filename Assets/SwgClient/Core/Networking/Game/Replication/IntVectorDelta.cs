using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 DeltaVector<int> operations, used by creature HAM and wounds.
    internal sealed class IntVectorDelta
    {
        readonly List<(byte Operation, ushort Index, int Value)> changes = new List<(byte, ushort, int)>();

        public static IntVectorDelta ReadFrom(ByteReader reader)
        {
            var result = new IntVectorDelta();
            int count = reader.Count();
            reader.U32(); // Server update counter.
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                ushort index = operation == 4 ? (ushort)0 : reader.U16();
                int value = 0;
                if (operation == 1 || operation == 2)
                    value = unchecked((int)reader.U32());
                else if (operation != 0 && operation != 4)
                    throw new InvalidDataException("Unsupported integer vector operation " + operation);
                result.changes.Add((operation, index, value));
            }
            return result;
        }

        public int[] Apply(int[] values)
        {
            var result = new List<int>(values);
            foreach (var change in changes)
            {
                switch (change.Operation)
                {
                    case 0: result.RemoveAt(change.Index); break;
                    case 1: result.Insert(change.Index, change.Value); break;
                    case 2: result[change.Index] = change.Value; break;
                    case 4: result.Clear(); break;
                }
            }
            return result.ToArray();
        }
    }
}
