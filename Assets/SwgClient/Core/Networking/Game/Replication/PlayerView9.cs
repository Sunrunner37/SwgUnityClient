using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 PlayerObjectMessage9: owner-only abilities followed by crafting, social and consumption state.
    internal static class PlayerView9
    {
        public static Action<List<string>> ReadBaseline(ByteReader r)
        {
            var abilities = Strings(r);
            r.Skip(16);
            ReplicationReader.SkipVector(r, 8);
            r.Skip(8);
            Strings(r);
            Strings(r);
            r.Skip(40);
            ReplicationReader.RequireEnd(r);
            return list =>
            {
                list.Clear();
                list.AddRange(abilities);
            };
        }

        static List<string> Strings(ByteReader r)
        {
            int count = r.Count();
            r.U32();
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
                list.Add(r.Ascii());
            return list;
        }

        public static bool TryReadDelta(ByteReader r, out Action<List<string>> update)
        {
            var updates = new List<Action<List<string>>>();
            int fields = r.U16();
            for (int i = 0; i < fields; i++)
            {
                ushort field = r.U16();
                if (field == 0)
                    ReadStringChanges(r, updates);
                else if (field == 4)
                    ReplicationReader.SkipVectorDelta(r, 8);
                else if (field == 7 || field == 8)
                    ReadStringChanges(r, null);
                else if (field == 3)
                    r.U64();
                else if (field == 16)
                    ReplicationReader.SkipVectorDelta(r, 0);
                else if (field <= 18)
                    r.U32();
                else
                    throw new InvalidDataException("Unsupported PLAY9 field " + field);
            }

            ReplicationReader.RequireEnd(r);
            update = null;
            if (updates.Count == 0)
                return false;
            update = list =>
            {
                foreach (var apply in updates)
                    apply(list);
            };
            return true;
        }

        static void ReadStringChanges(ByteReader r, List<Action<List<string>>> updates)
        {
            int count = r.Count();
            r.U32();
            for (int i = 0; i < count; i++)
            {
                byte op = r.Byte();
                if (op == 4)
                {
                    updates?.Add(list => list.Clear());
                    continue;
                }

                int index = r.U16();
                if (op == 0)
                    updates?.Add(list => list.RemoveAt(index));
                else if (op == 1 || op == 2)
                {
                    string value = r.Ascii();
                    if (op == 1)
                        updates?.Add(list => list.Insert(index, value));
                    else
                        updates?.Add(list => list[index] = value);
                }
                else
                    throw new InvalidDataException("Unsupported ability operation " + op);
            }
        }
    }
}
