using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class EquipmentDelta
    {
        readonly List<(byte Operation, ushort Index, WorldEquipment Item)> changes = new List<(byte, ushort, WorldEquipment)>();

        public static EquipmentDelta ReadFrom(ByteReader reader)
        {
            var result = new EquipmentDelta();
            int count = reader.Count();
            reader.U32(); // Server DeltaVector update counter.
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                ushort index = operation == 4 ? (ushort)0 : reader.U16();
                WorldEquipment item = default;
                if (operation == 1 || operation == 2)
                    item = CreatureView6.ReadEquipment(reader);
                else if (operation != 0 && operation != 4)
                    throw new InvalidDataException("Unsupported equipment delta operation " + operation);
                result.changes.Add((operation, index, item));
            }
            return result;
        }

        public WorldEquipment[] Apply(WorldEquipment[] equipment)
        {
            var result = new List<WorldEquipment>(equipment);
            foreach (var change in changes)
            {
                switch (change.Operation)
                {
                    case 0: result.RemoveAt(change.Index); break;
                    case 1: result.Insert(change.Index, change.Item); break;
                    case 2: result[change.Index] = change.Item; break;
                    case 4: result.Clear(); break;
                }
            }
            return result.ToArray();
        }
    }
}
