using Swg.Client.Core.World;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class CreatureView6
    {
        public static CreatureSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.U32();
            ReplicationReader.SkipVector(reader, sizeof(ulong)); // Tangible defenders.
            reader.U16(); // Level.
            reader.Ascii(); // Performance animation.
            reader.Ascii(); // Mood.
            reader.Skip(4 * sizeof(ulong)); // Weapon, group, inviter, invite counter.
            reader.U32(); // Guild.
            SwgId targetSwgId = reader.U64();
            reader.Byte(); // Mood ID.
            reader.U32(); // Performance start time.
            reader.U32(); // Performance type.
            var ham = ReplicationReader.ReadIntVector(reader);
            var maxHam = ReplicationReader.ReadIntVector(reader);
            int count = reader.Count();
            reader.U32(); // Equipment update counter.
            var equipment = new WorldEquipment[count];
            for (int i = 0; i < count; i++)
                equipment[i] = ReadEquipment(reader);
            string appearance = reader.Ascii();
            reader.Byte(); // Frozen.
            return new CreatureSceneObjectUpdate
            {
                TargetSwgId = targetSwgId,
                Ham = new HamPoolUpdate { Current = ham, Maximum = maxHam },
                Equipment = equipment,
                AlternateAppearance = appearance
            };
        }

        internal static WorldEquipment ReadEquipment(ByteReader reader) => new WorldEquipment(ReplicationReader.Customization(reader), reader.U32(), reader.U64(), reader.U32());
        public static bool ReadDeltaField(ushort field, ByteReader reader, CreatureSceneObjectUpdate update)
        {
            switch (field)
            {
                case 0:
                    reader.U32();
                    break;
                case 1:
                    ReplicationReader.SkipVectorDelta(reader, sizeof(ulong));
                    break; // Defenders.
                case 2:
                    reader.U16();
                    break; // Level.
                case 3:
                case 4:
                    reader.Ascii();
                    break; // Performance and mood.
                case 5:
                case 6:
                    reader.U64();
                    break; // Weapon, group.
                case 9:
                    update.TargetSwgId = reader.U64();
                    break;
                case 7:
                    reader.Skip(2 * sizeof(ulong));
                    break; // Group invitation.
                case 8:
                case 11:
                case 12:
                    reader.U32();
                    break;
                case 10:
                case 17:
                    reader.Byte();
                    break;
                case 13:
                    (update.Ham ??= new HamPoolUpdate()).CurrentDelta = IntVectorDelta.ReadFrom(reader);
                    break;
                case 14:
                    (update.Ham ??= new HamPoolUpdate()).MaximumDelta = IntVectorDelta.ReadFrom(reader);
                    break;
                case 15:
                    update.EquipmentDelta = EquipmentDelta.ReadFrom(reader);
                    break;
                case 16:
                    update.AlternateAppearance = reader.Ascii();
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
