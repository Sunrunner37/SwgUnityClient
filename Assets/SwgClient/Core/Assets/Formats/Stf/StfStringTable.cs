using System.Collections.Generic;
using System.IO;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Stf
{
    /// <summary>
    /// STF (String Table File) is SWG's localization format: numeric entries hold UTF-16 text,
    /// and a separate name table maps string keys to those entries.
    /// </summary>
    /// <seealso href="https://docs.rs/swg_stf/latest/swg_stf/">SWG STF format documentation</seealso>
    public static class StfStringTable
    {
        public static Dictionary<string, string> Parse(byte[] b)
        {
            var r = new ByteReader(b);
            if (r.U32() != 0xabcd || r.Byte() != 1)
                throw new InvalidDataException("Invalid STF header");
            r.U32();
            int count = checked((int)r.U32());
            if (count < 0 || count > 100000)
                throw new InvalidDataException("STF count limit exceeded");
            var values = new Dictionary<uint, string>();
            for (int i = 0; i < count; i++)
            {
                uint id = r.U32();
                r.U32();
                int length = checked((int)r.U32());
                if (length > 1000000)
                    throw new InvalidDataException("STF string too long");
                values[id] = Encoding.Unicode.GetString(r.Take(checked(length * 2)));
            }

            var result = new Dictionary<string, string>();
            for (int i = 0; i < count; i++)
            {
                uint id = r.U32();
                int n = checked((int)r.U32());
                result[Encoding.UTF8.GetString(r.Take(n))] = values[id];
            }

            return result;
        }
    }
}
