using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 CreatureObjectMessage1: owner-only currency, base HAM and skills.
    internal static class CreatureView1
    {
        public static CreatureSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new CreatureSceneObjectUpdate { BankCredits = unchecked((int)reader.U32()), CashCredits = unchecked((int)reader.U32()) };
            update.Ham = new HamPoolUpdate { Base = ReplicationReader.ReadIntVector(reader) };
            int count = reader.Count();
            reader.U32();
            for (int i = 0; i < count; i++)
                reader.Ascii();
            return update;
        }
        public static bool ReadDeltaField(ushort field, ByteReader reader, CreatureSceneObjectUpdate update)
        {
            if (field == 0)
                update.BankCredits = unchecked((int)reader.U32());
            else if (field == 1)
                update.CashCredits = unchecked((int)reader.U32());
            else if (field == 2)
                (update.Ham ??= new HamPoolUpdate()).BaseDelta = IntVectorDelta.ReadFrom(reader);
            else
                return false;
            return true;
        }
    }
}
