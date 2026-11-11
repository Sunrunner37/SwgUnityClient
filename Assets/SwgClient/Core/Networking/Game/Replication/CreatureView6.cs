using Swg.Client.Core.World;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class CreatureView6
    {
        public static GameObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.U32();
            ReplicationReader.SkipVector(reader, sizeof(ulong)); // Tangible defenders.
            reader.U16(); // Level.
            reader.Ascii(); // Performance animation.
            reader.Ascii(); // Mood.
            reader.Skip(4 * sizeof(ulong)); // Weapon, group, inviter, invite counter.
            reader.U32(); // Guild.
            reader.U64(); // Target.
            reader.Byte(); // Mood ID.
            reader.U32(); // Performance start time.
            reader.U32(); // Performance type.
            ReplicationReader.SkipVector(reader, sizeof(uint)); // HAM.
            ReplicationReader.SkipVector(reader, sizeof(uint)); // Maximum HAM.
            int count = reader.Count();
            reader.U32(); // Equipment update counter.
            var equipment = new WorldEquipment[count];
            for (int i = 0; i < count; i++)
                equipment[i] = ReadEquipment(reader);
            string appearance = reader.Ascii();
            reader.Byte(); // Frozen.
            return new GameObjectUpdate
            {
                Equipment = equipment,
                AlternateAppearance = appearance
            };
        }
        internal static WorldEquipment ReadEquipment(ByteReader reader) =>
            new WorldEquipment(ReplicationReader.Customization(reader), reader.U32(), reader.U64(), reader.U32());

        public static bool ReadDeltaField(ushort field, ByteReader reader, GameObjectUpdate update)
        {
            switch (field)
            {
                case 0: reader.U32(); break;
                case 1: SkipVectorDelta(reader, sizeof(ulong)); break; // Defenders.
                case 2: reader.U16(); break; // Level.
                case 3:
                case 4: reader.Ascii(); break; // Performance and mood.
                case 5:
                case 6:
                case 9: reader.U64(); break; // Weapon, group, target.
                case 7: reader.Skip(2 * sizeof(ulong)); break; // Group invitation.
                case 8:
                case 11:
                case 12: reader.U32(); break;
                case 10:
                case 17: reader.Byte(); break;
                case 13:
                case 14: SkipVectorDelta(reader, sizeof(uint)); break; // HAM.
                case 15: update.EquipmentDelta = EquipmentDelta.ReadFrom(reader); break;
                case 16: update.AlternateAppearance = reader.Ascii(); break;
                default: return false;
            }
            return true;
        }

        static void SkipVectorDelta(ByteReader reader, int elementSize)
        {
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
            {
                byte operation = reader.Byte();
                if (operation == 4)
                    continue;
                reader.U16();
                if (operation == 1 || operation == 2)
                    reader.Skip(elementSize);
                else if (operation != 0)
                    throw new System.IO.InvalidDataException("Unsupported creature vector operation " + operation);
            }
        }
    }
}
