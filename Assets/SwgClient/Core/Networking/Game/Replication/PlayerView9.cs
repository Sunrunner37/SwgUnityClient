using Swg.Client.Core.World;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 PlayerObjectMessage9: owner-only abilities followed by crafting, social and consumption state.
    internal static class PlayerView9
    {
        public static (Action<List<string>> Abilities, Action<List<uint>> Schematics, Action<CurrentPlayer> Details, Action<CraftingProgress> Crafting) ReadBaseline(ByteReader r)
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
            r.U32(); // Language.
            int food = unchecked((int)r.U32());
            int foodMaximum = unchecked((int)r.U32());
            int drink = unchecked((int)r.U32());
            int drinkMaximum = unchecked((int)r.U32());
            r.Skip(20);
            ReplicationReader.RequireEnd(r);
            return (list => { list.Clear(); list.AddRange(abilities); },
                list => { list.Clear(); list.AddRange(schematics); },
                player => player.CharacterSheet.Update(data => { data.Food = food; data.FoodMaximum = foodMaximum; data.Drink = drink; data.DrinkMaximum = drinkMaximum; }, CharacterSheetChange.Status), null);
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

        public static bool TryReadDelta(ByteReader r, out (Action<List<string>> Abilities, Action<List<uint>> Schematics, Action<CurrentPlayer> Details, Action<CraftingProgress> Crafting) update)
        {
            var updates = new List<Action<List<string>>>();
            var schematicUpdates = new List<Action<List<uint>>>();
            var details = new List<Action<CharacterSheetData>>();
            var crafting = new List<Action<CraftingProgress>>();
            int fields = r.U16();
            // Core3 finishStage2 inserts field 5 directly, so its count omits that field.
            // Decode the known owner fields through the payload boundary.
            for (int i = 0; i < fields || r.Remaining > 0; i++)
            {
                ushort field = r.U16();
                if (field >= 10 && field <= 13)
                {
                    int value = unchecked((int)r.U32());
                    details.Add(data => { switch (field) { case 10: data.Food = value; break; case 11: data.FoodMaximum = value; break; case 12: data.Drink = value; break; case 13: data.DrinkMaximum = value; break; } });
                }
                else if (field == 0)
                    ReadStringChanges(r, updates);
                else if (field == 4)
                    ReadSchematicChanges(r, schematicUpdates);
                else if (field == 7 || field == 8)
                    ReadStringChanges(r, null);
                else if (field == 3)
                {
                    var station = new SwgId(r.U64());
                    crafting.Add(data => data.StationSwgId = station);
                }
                else if (field == 1 || field == 2 || field == 5)
                {
                    int value = (int)r.U32();
                    crafting.Add(data => { switch (field) { case 1: data.ExperimentationEnabled = value == 3; break; case 2: data.ServerStage = value; break; case 5: data.ExperimentationPoints = Math.Max(0, value); break; } });
                }
                else if (field == 16)
                    ReplicationReader.SkipVectorDelta(r, 0);
                else if (field <= 18)
                    r.U32();
                else
                    throw new InvalidDataException("Unsupported PLAY9 field " + field);
            }

            ReplicationReader.RequireEnd(r);
            update = (updates.Count == 0 ? null : list => { foreach (var apply in updates) apply(list); },
                schematicUpdates.Count == 0 ? null : list => { foreach (var apply in schematicUpdates) apply(list); },
                details.Count == 0 ? null : player => player.CharacterSheet.Update(data => { foreach (var apply in details) apply(data); }, CharacterSheetChange.Status),
                crafting.Count == 0 ? null : progress => progress.Update(data => { foreach (var apply in crafting) apply(data); }));
            return updates.Count > 0 || schematicUpdates.Count > 0 || details.Count > 0 || crafting.Count > 0;
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

                if (op == 3)
                {
                    // Core3 PlayerList::insertToDeltaMessage refreshes the whole social list.
                    // Its list counter counts the refresh marker plus every replacement string.
                    int length = r.U16();
                    var values = new string[length];
                    for (int item = 0; item < length; item++) values[item] = r.Ascii();
                    updates?.Add(list => { list.Clear(); list.AddRange(values); });
                    i += length;
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
