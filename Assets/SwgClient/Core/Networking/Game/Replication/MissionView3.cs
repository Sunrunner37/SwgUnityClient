using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 MissionObjectMessage3 and MissionObjectDeltaMessage3.
    internal static class MissionView3
    {
        static string StringId(ByteReader reader)
        {
            var reference = StringIdReference.ReadFrom(reader);
            return reference.File.Length > 0 ? "@" + reference.File + ":" + reference.Key : reference.Key;
        }
        public static MissionSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new MissionSceneObjectUpdate();
            for (ushort field = 0; field <= 16; field++) ReadDeltaField(field, reader, update);
            return update;
        }
        public static SceneObjectUpdate ReadBaseline6(ByteReader reader)
        {
            reader.U32(); // Scene-object detail level (0x79), not mission content.
            return new MissionSceneObjectUpdate();
        }
        public static bool ReadDeltaField(ushort field, ByteReader reader, MissionSceneObjectUpdate update)
        {
            switch (field)
            {
                case 0: reader.Float(); break;
                case 1: update.Name = StringId(reader); break;
                case 2: string name = reader.Unicode(); if (name.Length > 0) update.Name = name; break;
                case 3: case 4: reader.U32(); break;
                case 5: update.Difficulty = unchecked((int)reader.U32()); break;
                case 6: update.EndPosition = PacketFieldCodec.Vector(reader); reader.U64(); update.EndPlanetCrc = reader.U32(); break;
                case 7: update.Creator = reader.Unicode(); break;
                case 8: update.Reward = unchecked((int)reader.U32()); break;
                case 9: update.StartPosition = PacketFieldCodec.Vector(reader); reader.U64(); update.StartPlanetCrc = reader.U32(); break;
                case 10: update.TargetTemplateCrc = reader.U32(); break;
                case 11: update.Description = StringId(reader); break;
                case 12: update.Title = StringId(reader); break;
                case 13: update.RefreshCounter = unchecked((int)reader.U32()); break;
                case 14: update.MissionTypeCrc = reader.U32(); break;
                case 15: update.TargetName = reader.Ascii(); break;
                case 16:
                    var waypoint = ChatWaypoint.ReadFrom(reader);
                    // ChatWaypoint exposes x/north/height; application positions use x/height/north.
                    update.HasWaypoint = true;
                    update.Waypoint = waypoint.CellSwgId == 0 ? null : new Waypoint(waypoint.CellSwgId, new Vector3(waypoint.Position.x, waypoint.Position.z, waypoint.Position.y), waypoint.PlanetCrc, waypoint.Name, (WaypointColor)waypoint.Color, waypoint.Active != 0);
                    break;
                default: return false;
            }
            return true;
        }
    }
}

