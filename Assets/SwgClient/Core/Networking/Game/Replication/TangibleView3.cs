using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class TangibleView3
    {
        public static GameObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new GameObjectUpdate();
            ReadBaseline(reader, update);
            return update;
        }

        public static void ReadBaseline(ByteReader reader, GameObjectUpdate update)
        {
            reader.Float(); // Complexity.
            string file = reader.Ascii();
            reader.U32();
            string key = reader.Ascii();
            string custom = reader.Unicode();
            update.Name = custom.Length > 0 ? custom : (file.Length > 0 ? "@" + file + ":" + key : key);
            reader.U32(); // Volume.
            update.Customization = ReplicationReader.Customization(reader);
            ReplicationReader.SkipVector(reader, sizeof(uint)); // Visible components.
            reader.Skip(4 * sizeof(uint)); // Options, use count, damage, maximum condition.
            reader.Byte(); // Visibility.
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, GameObjectUpdate update)
        {
            switch (field)
            {
                case 0:
                    reader.Float();
                    break;
                case 1:
                    reader.Ascii();
                    reader.U32();
                    reader.Ascii();
                    break;
                case 2:
                    update.Name = reader.Unicode();
                    break;
                case 3:
                    reader.U32();
                    break;
                case 4:
                    update.Customization = ReplicationReader.Customization(reader);
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                    reader.U32();
                    break;
                case 10:
                    reader.Byte();
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}
