using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;
using UnityEngine;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 PlayerObjectMessage8 and WaypointList: map operations carry the entire waypoint even on removal.
    internal static class PlayerView8
    {
        public static Action<WorldState> ReadBaseline(ByteReader reader)
        {
            var experience = SkillReplication.ReadValues(reader, true, false);
            var updates = ReadWaypoints(reader);
            reader.Skip(8); // Current and maximum Force.
            ReplicationReader.SkipVector(reader, 1); // Completed quests.
            ReplicationReader.SkipVector(reader, 1); // Active quests.
            SkipQuests(reader);
            reader.Skip(8); // Reserved map count and counter.
            ReplicationReader.RequireEnd(reader);
            return state =>
            {
                state.UpdateExperience(experience);
                state.UpdateWaypoints(list => { list.Clear(); updates(list); });
            };
        }

        public static Action<WorldState> ReadDelta(ByteReader reader)
        {
            var updates = new List<Action<WorldState>>();
            int fields = reader.U16();
            for (int i = 0; i < fields; i++)
            {
                switch (reader.U16())
                {
                    case 0:
                        var experience = SkillReplication.ReadValues(reader, false, false);
                        updates.Add(state => state.UpdateExperience(experience));
                        break;
                    case 1:
                        var waypoints = ReadWaypoints(reader);
                        updates.Add(state => state.UpdateWaypoints(waypoints));
                        break;
                    case 2:
                    case 3: reader.U32(); break;
                    case 4:
                    case 5: ReplicationReader.SkipVectorDelta(reader, 1); break;
                    case 6: SkipQuests(reader); break;
                    default: throw new InvalidDataException("Unsupported PLAY8 delta field");
                }
            }
            ReplicationReader.RequireEnd(reader);
            return state =>
            {
                foreach (var update in updates)
                    update(state);
            };
        }

        static Action<Dictionary<SwgId, Waypoint>> ReadWaypoints(ByteReader reader)
        {
            int count = reader.Count();
            reader.U32(); // Map update counter.
            var entries = new (byte Operation, Waypoint Waypoint)[count];
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                if (operation > 2)
                    throw new InvalidDataException("Unsupported waypoint map operation " + operation);
                SwgId swgId = reader.U64();
                reader.U32(); // Legacy cell ID.
                var position = new Vector3(reader.Float(), reader.Float(), reader.Float()); // X, height, north.
                reader.U64(); // Reserved.
                uint planet = reader.U32();
                string name = reader.Unicode();
                reader.U64(); // Repeated object ID.
                var color = (WaypointColor)reader.Byte();
                bool active = reader.Byte() != 0;
                entries[i] = (operation, new Waypoint(swgId, position, planet, name, color, active));
            }
            return list =>
            {
                foreach (var entry in entries)
                    if (entry.Operation == 1)
                        list.Remove(entry.Waypoint.SwgId);
                    else
                        list[entry.Waypoint.SwgId] = entry.Waypoint;
            };
        }

        static void SkipQuests(ByteReader reader)
        {
            int count = reader.Count();
            reader.U32();
            // Map operation, CRC key, owner ID, two step masks, completion flag, and quest counter.
            reader.Skip(checked(count * (1 + 4 + 8 + 2 + 2 + 1 + 4)));
        }
    }
}
