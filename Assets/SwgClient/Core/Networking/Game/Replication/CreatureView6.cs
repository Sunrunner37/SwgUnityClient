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
            var equipment = new GameEquipment[count];
            for (int i = 0; i < count; i++)
                equipment[i] = new GameEquipment(ReplicationReader.Customization(reader), reader.U32(), reader.U64(), reader.U32());
            string appearance = reader.Ascii();
            reader.Byte(); // Frozen.
            return new GameObjectUpdate
            {
                Equipment = equipment,
                AlternateAppearance = appearance
            };
        }
    }
}
