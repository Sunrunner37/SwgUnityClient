using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Iff
{
    /// <summary>
    /// IFF (Interchange File Format) stores tagged chunks nested inside FORM containers.
    /// SWG uses a variant for templates, meshes and skeletons; this reader follows its observed chunk layout.
    /// </summary>
    /// <seealso href="https://eblong.com/zarf/blorb/iff.html">Original EA IFF 85 specification</seealso>
    public sealed class IffNode
    {
        public string Tag;
        public string Kind;
        public byte[] Data;
        public readonly List<IffNode> Children = new List<IffNode>();
        public static List<IffNode> Parse(byte[] data, int depth = 0)
        {
            if (depth > 32)
                throw new InvalidDataException("IFF nesting limit exceeded");
            var list = new List<IffNode>();
            var r = new ByteReader(data);
            while (r.Remaining > 0)
            {
                string tag = Encoding.ASCII.GetString(r.Take(4));
                int length = checked((int)r.Net32());
                var payload = r.Take(length);
                var n = new IffNode
                {
                    Tag = tag,
                    Data = payload
                };
                if (tag == "FORM")
                {
                    if (length < 4)
                        throw new InvalidDataException("Truncated IFF form");
                    n.Kind = Encoding.ASCII.GetString(payload, 0, 4);
                    var body = new byte[length - 4];
                    Buffer.BlockCopy(payload, 4, body, 0, body.Length);
                    n.Children.AddRange(Parse(body, depth + 1));
                }

                list.Add(n);
            }

            return list;
        }

        public static IffNode Form(IEnumerable<IffNode> nodes, string kind)
        {
            foreach (var n in nodes)
            {
                if (n.Kind == kind)
                    return n;
                var found = Form(n.Children, kind);
                if (found != null)
                    return found;
            }

            return null;
        }

        public byte[] Chunk(string tag) => Children.FirstOrDefault(n => n.Tag == tag)?.Data ?? throw new InvalidDataException("Missing IFF chunk " + tag);
        public static List<string> References(byte[] bytes, string prefix, string suffix)
        {
            var result = new List<string>();
            var p = Encoding.ASCII.GetBytes(prefix);
            for (int i = 0; i <= bytes.Length - p.Length; i++)
            {
                int j = 0;
                while (j < p.Length && bytes[i + j] == p[j])
                    j++;
                if (j != p.Length)
                    continue;
                int end = Array.IndexOf(bytes, (byte)0, i);
                if (end < 0)
                    continue;
                string path = Encoding.ASCII.GetString(bytes, i, end - i);
                if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && !result.Contains(path))
                    result.Add(path);
            }

            return result;
        }
    }
}
