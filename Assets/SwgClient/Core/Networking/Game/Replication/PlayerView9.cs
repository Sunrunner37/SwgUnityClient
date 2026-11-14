using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 PlayerObjectMessage9: owner-only abilities followed by crafting, social and consumption state.
    internal static class PlayerView9
    {
        public static (Action<List<string>> Abilities, Action<List<uint>> Schematics) ReadBaseline(ByteReader r)
        {
            var abilities = Strings(r);
            r.Skip(16);
            int schematicCount = r.Count();
            r.U32();
            var schematics = new List<uint>(schematicCount);
            for (int i = 0; i < schematicCount; i++)
                schematics.Add(ReadSchematic(r));
            r.Skip(8);
            Strings(r);
            Strings(r);
            r.Skip(40);
            ReplicationReader.RequireEnd(r);
            return (list => { list.Clear(); list.AddRange(abilities); },
                list => { list.Clear(); list.AddRange(schematics); });
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

        public static bool TryReadDelta(ByteReader r, out (Action<List<string>> Abilities, Action<List<uint>> Schematics) update)
        {
            var updates = new List<Action<List<string>>>();
            var schematicUpdates = new List<Action<List<uint>>>();
            int fields = r.U16();
            for (int i = 0; i < fields; i++)
            {
                ushort field = r.U16();
                if (field == 0)
                    ReadStringChanges(r, updates);
                else if (field == 4)
                    ReadSchematicChanges(r, schematicUpdates);
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
            update = (updates.Count == 0 ? null : list => { foreach (var apply in updates) apply(list); },
                schematicUpdates.Count == 0 ? null : list => { foreach (var apply in schematicUpdates) apply(list); });
            return updates.Count > 0 || schematicUpdates.Count > 0;
        }

        static uint ReadSchematic(ByteReader reader)
        {
            uint clientCrc = reader.U32();
            reader.U32(); // Core3 SchematicList writes the client CRC twice.
            return clientCrc;
        }
        static void ReadSchematicChanges(ByteReader reader, List<Action<List<uint>>> updates)
        {
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                if (operation == 4) { updates.Add(list => list.Clear()); continue; }
                int index = reader.U16();
                if (operation == 0) updates.Add(list => list.RemoveAt(index));
                else if (operation == 1 || operation == 2)
                {
                    uint crc = ReadSchematic(reader);
                    if (operation == 1) updates.Add(list => list.Insert(index, crc));
                    else updates.Add(list => list[index] = crc);
                }
                else throw new InvalidDataException("Unsupported schematic operation " + operation);
            }
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
