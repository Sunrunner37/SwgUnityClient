using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class CreatureView3
    {
        public static GameObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new GameObjectUpdate();
            TangibleView3.ReadBaseline(reader, update);
            update.Posture = reader.Byte();
            reader.Byte(); // Faction rank.
            reader.U64(); // Creature link.
            update.Height = reader.Float();
            reader.U32(); // Shock wounds.
            reader.U64(); // State bitmask.
            update.Wounds = ReplicationReader.ReadIntVector(reader);
            return update;
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, GameObjectUpdate update)
        {
            if (TangibleView3.ReadDeltaField(field, reader, update))
                return true;
            switch (field)
            {
                case 11:
                    update.Posture = reader.Byte();
                    break;
                case 12:
                    reader.Byte();
                    break;
                case 13:
                case 16:
                    reader.U64();
                    break;
                case 14:
                    update.Height = reader.Float();
                    break;
                case 15:
                    reader.U32();
                    break;
                case 17:
                    update.WoundsDelta = IntVectorDelta.ReadFrom(reader);
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
