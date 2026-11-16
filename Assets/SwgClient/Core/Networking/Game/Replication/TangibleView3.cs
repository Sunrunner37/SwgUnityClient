using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class TangibleView3
    {
        public static TangibleSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new TangibleSceneObjectUpdate();
            ReadBaseline(reader, update);
            return update;
        }

        public static void ReadBaseline(ByteReader reader, TangibleSceneObjectUpdate update)
        {
            reader.Float(); // Complexity.
            string file = reader.Ascii();
            reader.U32();
            string key = reader.Ascii();
            string custom = reader.Unicode();
            update.Name = custom.Length > 0 ? custom : (file.Length > 0 ? "@" + file + ":" + key : key);
            update.Volume = unchecked((int)reader.U32());
            update.Customization = ReplicationReader.Customization(reader);
            ReplicationReader.SkipVector(reader, sizeof(uint)); // Visible components.
            update.Options = reader.U32();
            update.Quantity = unchecked((int)reader.U32());
            update.ConditionDamage = unchecked((int)reader.U32());
            update.MaximumCondition = unchecked((int)reader.U32());
            reader.Byte(); // Visibility.
        }

        public static bool ReadDeltaField(ushort field, ByteReader reader, TangibleSceneObjectUpdate update)
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
                    update.Volume = unchecked((int)reader.U32());
                    break;
                case 4:
                    update.Customization = ReplicationReader.Customization(reader);
                    break;
                case 8:
                    update.ConditionDamage = unchecked((int)reader.U32());
                    break;
                case 9:
                    update.MaximumCondition = unchecked((int)reader.U32());
                    break;
                case 6:
                    update.Options = reader.U32();
                    break;
                case 7:
                    update.Quantity = unchecked((int)reader.U32());
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
