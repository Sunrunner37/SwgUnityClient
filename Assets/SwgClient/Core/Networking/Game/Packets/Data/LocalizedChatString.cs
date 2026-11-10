using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct LocalizedChatString
    {
        public readonly StringIdReference StringId;
        public readonly ChatToken User;
        public readonly ChatToken Target;
        public readonly ChatToken Object;
        public readonly int IntegerParameter;
        public readonly float FloatParameter;
        public readonly byte UnknownFlag;
        LocalizedChatString(ByteReader reader)
        {
            this = default;
            StringId = StringIdReference.ReadFrom(reader);
            User = ChatToken.ReadFrom(reader);
            Target = ChatToken.ReadFrom(reader);
            Object = ChatToken.ReadFrom(reader);
            IntegerParameter = unchecked((int)reader.U32());
            FloatParameter = reader.Float();
            UnknownFlag = reader.Byte();
        }

        public static LocalizedChatString ReadFrom(ByteReader reader) => new LocalizedChatString(reader);
        public void WriteTo(BinaryWriter writer)
        {
            StringId.WriteTo(writer);
            User.WriteTo(writer);
            Target.WriteTo(writer);
            Object.WriteTo(writer);
            writer.Write(IntegerParameter);
            writer.Write(FloatParameter);
            writer.Write(UnknownFlag);
        }
    }
}
