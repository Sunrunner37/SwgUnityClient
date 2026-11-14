using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 CreatureObjectMessage4 / CreatureObjectDeltaMessage4: movement parameters sent to the owner.
    internal static class CreatureView4
    {
        public static CreatureSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new CreatureSceneObjectUpdate
            {
                AccelerationMultiplierBase = reader.Float(),
                AccelerationMultiplier = reader.Float()
            };
            update.Ham = new HamPoolUpdate { Encumbrances = ReplicationReader.ReadIntVector(reader) };
            update.SkillModifiers = SkillReplication.ReadValues(reader, true, true);
            reader.Float(); // Speed multiplier base (Core3 validates run speed against its mod value).
            update.SpeedMultiplier = reader.Float();
            reader.U64(); // Listen ID.
            update.RunSpeed = reader.Float();
            reader.Skip(8); // Slope angle / percent.
            update.TurnScale = reader.Float();
            reader.Skip(8); // Walk speed / water modifier.
            ReplicationReader.SkipVector(reader, sizeof(ulong)); // Unused group critical objects.
            return update;
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, CreatureSceneObjectUpdate update)
        {
            switch (field)
            {
                case 0:
                    update.AccelerationMultiplierBase = reader.Float();
                    break;
                case 1:
                    update.AccelerationMultiplier = reader.Float();
                    break;
                case 4:
                case 8:
                case 9:
                case 11:
                case 12:
                    reader.Float();
                    break;
                case 2:
                    (update.Ham ??= new HamPoolUpdate()).EncumbrancesDelta = IntVectorDelta.ReadFrom(reader);
                    break;
                case 3:
                    update.SkillModifiers = SkillReplication.ReadValues(reader, false, true);
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

    }
}
