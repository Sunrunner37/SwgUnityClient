using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct SchematicChoice
    {
        public readonly uint ClientCrc;
        public readonly uint ServerCrc;
        public readonly uint ToolTab;
        SchematicChoice(ByteReader reader)
        {
            this = default;
            ClientCrc = reader.U32();
            ServerCrc = reader.U32();
            ToolTab = reader.U32();
        }

        public static SchematicChoice ReadFrom(ByteReader reader) => new SchematicChoice(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ClientCrc);
            writer.Write(ServerCrc);
            writer.Write(ToolTab);
        }
    }
}
