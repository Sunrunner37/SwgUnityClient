using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    // Chat/mail parameters are measured in UTF-16 code units, but contain binary typed records.
    public readonly struct ChatParameterBlock
    {
        public readonly ChatParameterData[] Parameters;
        public readonly bool Unframed;
        ChatParameterBlock(ChatParameterData[] parameters, bool unframed)
        {
            Parameters = parameters;
            Unframed = unframed;
        }

        public static ChatParameterBlock ReadFrom(ByteReader reader)
        {
            // The legacy persistent-mail constructor writes one parameter without a length prefix.
            if (ChatParameterData.IsHeader(reader))
                return new ChatParameterBlock(new[] { ChatParameterData.ReadFrom(reader) }, true);
            uint units = reader.U32();
            var block = new ByteReader(reader.Take(checked((int)units * 2)));
            var parameters = new List<ChatParameterData>();
            while (block.Remaining > 0)
                parameters.Add(ChatParameterData.ReadFrom(block));
            return new ChatParameterBlock(parameters.ToArray(), false);
        }

        public void WriteTo(BinaryWriter writer)
        {
            if (Unframed)
            {
                Parameters[0].WriteTo(writer);
                return;
            }

            using var stream = new MemoryStream();
            using var block = new BinaryWriter(stream);
            foreach (var parameter in Parameters)
                parameter.WriteTo(block);
            if ((stream.Length & 1) != 0)
                throw new InvalidDataException("Unaligned chat parameters");
            writer.Write(checked((uint)(stream.Length / 2)));
            writer.Write(stream.ToArray());
        }
    }
}
