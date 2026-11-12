using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 CreatureObjectMessage4 / CreatureObjectDeltaMessage4: movement parameters sent to the owner.
    internal static class CreatureView4
    {
        public static GameObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.Skip(8); // Acceleration multipliers.
            ReplicationReader.SkipVector(reader, sizeof(uint)); // Encumbrances.
            SkipSkillMods(reader, false);
            reader.Float(); // Speed multiplier base (Core3 validates run speed against its mod value).
            var update = new GameObjectUpdate
            {
                SpeedMultiplier = reader.Float()
            };
            reader.U64(); // Listen ID.
            update.RunSpeed = reader.Float();
            reader.Skip(8); // Slope angle / percent.
            update.TurnScale = reader.Float();
            reader.Skip(8); // Walk speed / water modifier.
            ReplicationReader.SkipVector(reader, sizeof(ulong)); // Unused group critical objects.
            return update;
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, GameObjectUpdate update)
        {
            switch (field)
            {
                case 0:
                case 1:
                case 4:
                case 8:
                case 9:
                case 11:
                case 12:
                    reader.Float();
                    break;
                case 2:
                    ReplicationReader.SkipVectorDelta(reader, sizeof(uint));
                    break;
                case 3:
                    SkipSkillMods(reader, true);
                    break;
                case 5:
                    update.SpeedMultiplier = reader.Float();
                    break;
                case 6:
                    reader.U64();
                    break;
                case 7:
                    update.RunSpeed = reader.Float();
                    break;
                case 10:
                    update.TurnScale = reader.Float();
                    break;
                case 13:
                    ReplicationReader.SkipVectorDelta(reader, sizeof(ulong));
                    break;
                default:
                    return false;
            }

            return true;
        }

        static void SkipSkillMods(ByteReader reader, bool delta)
        {
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                reader.Ascii();
                if (!delta || operation == 0)
                    reader.Skip(8); // Skill value and bonus.
                else if (operation != 1)
                    throw new InvalidDataException("Unsupported skill-mod map operation " + operation);
            }
        }
    }
}
