using Swg.Client.Core.Assets.Formats.Binary;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class ManufactureSchematicView
    {
        public static ManufactureSchematicSceneObjectUpdate ReadBaseline3(ByteReader reader)
        {
            reader.Float();
            string file = reader.Ascii(); reader.U32(); string key = reader.Ascii();
            string custom = reader.Unicode();
            var update = new ManufactureSchematicSceneObjectUpdate
            {
                Name = custom.Length > 0 ? custom : "@" + file + ":" + key,
                DataSize = unchecked((int)reader.U32())
            };
            reader.U32(); // Manufacturing limit; examine attributes provide its display value.
            SkipValues(reader);
            reader.Unicode(); // Creator.
            reader.U32(); reader.Float(); // Version/volume.
            return update;
        }
        public static ManufactureSchematicSceneObjectUpdate ReadBaseline6(ByteReader reader)
        {
            reader.Skip(8);
            uint crc = reader.U32();
            reader.U16();
            return new ManufactureSchematicSceneObjectUpdate { DraftSchematicCrc = crc };
        }
        public static bool ReadDelta3(ushort field, ByteReader reader, ManufactureSchematicSceneObjectUpdate update)
        {
            switch (field)
            {
                case 0: update.Complexity = reader.Float(); return true;
                case 1: reader.Ascii(); reader.U32(); reader.Ascii(); return true;
                case 2: update.Name = reader.Unicode(); return true;
                case 3: update.DataSize = unchecked((int)reader.U32()); return true;
                case 4: reader.U32(); return true;
                case 5:
                    int count = reader.Count(); reader.U32();
                    for (int i = 0; i < count; i++)
                    {
                        reader.Byte(); string file = reader.Ascii(); reader.U32(); string key = reader.Ascii(); float value = reader.Float();
                        update.Crafting.Add(item => item.CraftingValues["@" + file + ":" + key] = value);
                    }
                    return true;
                case 6: reader.Unicode(); return true;
                case 7: reader.U32(); return true;
                case 8: reader.Float(); return true;
                default: return false;
            }
        }
        public static bool ReadDelta6(ushort field, ByteReader reader, ManufactureSchematicSceneObjectUpdate update)
        {
            if (field != 5) return false;
            reader.Byte(); // Core3's last-inserted slot notification; contents arrive in MSCO7.
            return true;
        }
        static void SkipValues(ByteReader reader)
        {
            int count = reader.Count(); reader.U32();
            for (int i = 0; i < count; i++)
            {
                reader.Byte(); reader.Ascii(); reader.U32(); reader.Ascii(); reader.Float();
            }
        }
    }
}
