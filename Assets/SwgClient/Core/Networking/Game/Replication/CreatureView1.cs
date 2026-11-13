using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 CreatureObjectMessage1: owner-only currency, base HAM and skills.
    internal static class CreatureView1
    {
        public static GameObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new GameObjectUpdate { BankCredits = unchecked((int)reader.U32()), CashCredits = unchecked((int)reader.U32()) };
            ReplicationReader.SkipVector(reader, sizeof(int));
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
                reader.Ascii();
            return update;
        }
        public static bool ReadDeltaField(ushort field, ByteReader reader, GameObjectUpdate update)
        {
            if (field == 0)
                update.BankCredits = unchecked((int)reader.U32());
            else if (field == 1)
                update.CashCredits = unchecked((int)reader.U32());
            else if (field == 2)
                ReplicationReader.SkipVectorDelta(reader, sizeof(int));
            else
                return false;
            return true;
        }
    }
}
