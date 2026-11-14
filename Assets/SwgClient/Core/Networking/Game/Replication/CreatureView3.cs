using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class CreatureView3
    {
        public static CreatureSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new CreatureSceneObjectUpdate();
            TangibleView3.ReadBaseline(reader, update);
            update.Posture = reader.Byte();
            reader.Byte(); // Faction rank.
            reader.U64(); // Creature link.
            update.Height = reader.Float();
            update.BattleFatigue = unchecked((int)reader.U32());
            update.States = reader.U64();
            update.Ham = new HamPoolUpdate { Wounds = ReplicationReader.ReadIntVector(reader) };
            return update;
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, CreatureSceneObjectUpdate update)
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
                case 16:
                    update.States = reader.U64();
                    break;
                case 13:
                    reader.U64();
                    break;
                case 14:
                    update.Height = reader.Float();
                    break;
                case 15:
                    update.BattleFatigue = unchecked((int)reader.U32());
                    break;
                case 17:
                    (update.Ham ??= new HamPoolUpdate()).WoundsDelta = IntVectorDelta.ReadFrom(reader);
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
