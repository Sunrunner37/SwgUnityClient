using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 ResourceContainerObjectMessage3/6: tangible fields, stack size and resource display name.
    internal static class ResourceContainerView
    {
        public static TangibleSceneObjectUpdate ReadBaseline3(ByteReader reader)
        {
            var update = TangibleView3.ReadBaseline(reader);
            update.Quantity = unchecked((int)reader.U32());
            reader.U64(); // Resource spawn ID.
            return update;
        }
        public static TangibleSceneObjectUpdate ReadBaseline6(ByteReader reader)
        {
            reader.Ascii();
            reader.U32();
            reader.Ascii();
            reader.Unicode();
            reader.U32(); // Maximum stack size.
            string type = reader.Ascii();
            reader.Unicode(); // Unique spawn name, distinct from the displayed resource type.
            return new TangibleSceneObjectUpdate { ResourceType = type };
        }
        public static bool ReadDelta3(ushort field, ByteReader reader, TangibleSceneObjectUpdate update)
        {
            if (TangibleView3.ReadDeltaField(field, reader, update))
                return true;
            if (field == 11)
                update.Quantity = unchecked((int)reader.U32());
            else if (field == 12)
                reader.U64();
            else
                return false;
            return true;
        }
    }
}
