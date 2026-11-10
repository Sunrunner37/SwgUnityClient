using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Cidm
{
    /// <summary>
    /// CIDM (customization ID manager) is an IFF table mapping packed customization IDs
    /// to variable names. Version 0001 DATA contains little-endian ushort/string pairs.
    /// </summary>
    public static class CidmFile
    {
        public static IReadOnlyDictionary<ushort, string> Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var form = IffNode.Form(roots, "CIDM") ?? throw new InvalidDataException("Missing customization ID manager");
            var version = form.Children.SingleOrDefault(n => n.Kind == "0001") ?? throw new InvalidDataException("Unsupported customization ID map version");
            var chunk = version.Children.SingleOrDefault(n => n.Tag == "DATA") ?? throw new InvalidDataException("Missing customization ID data");
            var reader = new ByteReader(chunk.Data);
            var ids = new Dictionary<ushort, string>();
            while (reader.Remaining != 0)
            {
                if (ids.Count >= 4096)
                    throw new InvalidDataException("Oversized customization ID map");
                ushort id = reader.U16();
                string name = ReadString(reader);
                if (name.Length == 0 || ids.ContainsKey(id))
                    throw new InvalidDataException("Invalid customization ID entry");
                ids.Add(id, name);
            }

            return new ReadOnlyDictionary<ushort, string>(ids);
        }

        static byte[] ReadBytes(ByteReader reader)
        {
            int start = reader.Position;
            while (reader.Byte() != 0)
                if (reader.Position - start > 65536)
                    throw new InvalidDataException("Oversized customization variable name");
            var result = new byte[reader.Position - start - 1];
            Buffer.BlockCopy(reader.Bytes, start, result, 0, result.Length);
            return result;
        }

        static string ReadString(ByteReader reader)
        {
            var bytes = ReadBytes(reader);
            if (bytes.Length > 4096 || bytes.Any(b => b > 127))
                throw new InvalidDataException("Invalid customization variable name");
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
