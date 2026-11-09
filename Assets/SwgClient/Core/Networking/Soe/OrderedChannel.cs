using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Soe
{
    internal sealed class OrderedChannel
    {
        ushort expected;
        public ushort? LastAck;
        readonly Dictionary<ushort, byte[]> pending = new Dictionary<ushort, byte[]>();
        readonly MemoryStream fragment = new MemoryStream();
        int? fragmentSize;
        public List<byte[]> Accept(byte[] b)
        {
            if (b.Length < 4)
                throw new InvalidDataException("Truncated reliable packet");
            ushort seq = (ushort)(b[2] << 8 | b[3]);
            ushort distance = (ushort)(seq - expected);
            var result = new List<byte[]>();
            if (distance >= 32768)
                return result;
            if (distance > 1024)
                throw new InvalidDataException("Receive window exceeded");
            if (!pending.ContainsKey(seq))
                pending.Add(seq, b);
            while (pending.TryGetValue(expected, out var p))
            {
                pending.Remove(expected);
                LastAck = expected;
                expected++;
                var r = new ByteReader(p)
                {
                    Position = 4
                };
                if (p[1] == 13)
                {
                    if (!fragmentSize.HasValue)
                    {
                        uint size = r.Net32();
                        if (size == 0 || size > SoeCodec.Limit)
                            throw new InvalidDataException("Invalid fragment size");
                        fragmentSize = (int)size;
                    }

                    byte[] part = r.Take(r.Remaining);
                    fragment.Write(part, 0, part.Length);
                    if (fragment.Length > fragmentSize.Value)
                        throw new InvalidDataException("Fragment length exceeded");
                    if (fragment.Length == fragmentSize.Value)
                    {
                        result.AddRange(SoeCodec.SplitMessages(fragment.ToArray()));
                        fragment.SetLength(0);
                        fragmentSize = null;
                    }
                }
                else
                {
                    if (fragmentSize.HasValue)
                        throw new InvalidDataException("Interrupted fragmented message");
                    result.AddRange(SoeCodec.SplitMessages(r.Take(r.Remaining)));
                }
            }

            return result;
        }
    }
}
