using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Core3 SkillList uses names (not vector indexes); XP and modifiers use DeltaVectorMap entries.</summary>
    internal static class SkillReplication
    {
        public static Action<List<string>> ReadSkills(ByteReader reader, bool baseline)
        {
            int count = reader.Count();
            reader.U32();
            var entries = new (byte Operation, string Name)[count];
            for (int i = 0; i < count; i++)
            {
                byte operation = baseline ? (byte)1 : reader.Byte();
                if (operation > 1) throw new InvalidDataException("Unsupported skill-list operation " + operation);
                entries[i] = (operation, reader.Ascii());
            }
            return skills =>
            {
                if (baseline) skills.Clear();
                foreach (var entry in entries)
                    if (entry.Operation == 0) skills.Remove(entry.Name);
                    else if (!skills.Contains(entry.Name)) skills.Add(entry.Name);
            };
        }

        public static Action<Dictionary<string, int>> ReadValues(ByteReader reader, bool baseline, bool modifiers)
        {
            int count = reader.Count();
            reader.U32();
            var entries = new (byte Operation, string Name, int Value)[count];
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                if (operation > 1) throw new InvalidDataException("Unsupported skill-map operation " + operation);
                string name = reader.Ascii();
                int value = unchecked((int)reader.U32());
                // Core3 serializes the value on removals too, including both SkillModEntry members.
                if (modifiers) value += unchecked((int)reader.U32());
                entries[i] = (operation, name, value);
            }
            return values =>
            {
                if (baseline) values.Clear();
                foreach (var entry in entries)
                    if (entry.Operation == 1) values.Remove(entry.Name);
                    else values[entry.Name] = entry.Value;
            };
        }
    }
}
