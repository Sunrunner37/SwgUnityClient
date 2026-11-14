using Swg.Client.Core.Assets.Formats.Binary;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class IntangibleView3
    {
        public static SceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.Float();
            string file = reader.Ascii(); reader.U32(); string key = reader.Ascii();
            string custom = reader.Unicode();
            reader.U32(); // Volume.
            reader.U32(); // Status.
            return new SceneObjectUpdate { Name = custom.Length > 0 ? custom : file.Length > 0 ? "@" + file + ":" + key : key };
        }
        public static bool ReadDeltaField(ushort field, ByteReader reader, SceneObjectUpdate update)
        {
            switch (field)
            {
                case 0: reader.Float(); return true;
                case 1: reader.Ascii(); reader.U32(); reader.Ascii(); return true;
                case 2: update.Name = reader.Unicode(); return true;
                case 3: case 4: reader.U32(); return true;
                default: return false;
            }
        }
    }
}
