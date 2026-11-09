using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Compression;

namespace Swg.Client.Core.Assets.Tre
{
    public sealed class TreAssetLoader : IDisposable
    {
        const int Limit = 256 * 1024 * 1024;
        sealed class Entry
        {
            public long Offset;
            public int Stored;
            public int Size;
            public uint Compression;
        }

        sealed class Archive : IDisposable
        {
            public string Path;
            public FileStream Stream;
            public readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            public Archive(string path)
            {
                Path = path;
                Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    var h = ReadBlock(Stream, 0, 36, 0, 36);
                    if (System.Text.Encoding.ASCII.GetString(h, 0, 8) != "EERT5000")
                        throw new InvalidDataException("Unsupported TRE version: " + path);
                    int count = checked((int)ByteReader.At(h, 8));
                    long offset = ByteReader.At(h, 12);
                    int stored = checked((int)ByteReader.At(h, 20));
                    var records = ReadBlock(Stream, offset, stored, ByteReader.At(h, 16), checked(count * 24));
                    var names = ReadBlock(Stream, offset + stored, checked((int)ByteReader.At(h, 28)), ByteReader.At(h, 24), checked((int)ByteReader.At(h, 32)));
                    long archiveLength = Stream.Length;
                    for (int i = 0; i < count; i++)
                    {
                        int p = i * 24;
                        var r = new ByteReader(names)
                        {
                            Position = checked((int)ByteReader.At(records, p + 20))
                        };
                        string name = Normalize(r.CString());
                        int size = checked((int)ByteReader.At(records, p + 4));
                        uint compression = ByteReader.At(records, p + 12);
                        var entry = new Entry
                        {
                            Offset = ByteReader.At(records, p + 8),
                            Size = size,
                            Compression = compression,
                            Stored = compression == 0 ? size : checked((int)ByteReader.At(records, p + 16))
                        };
                        if (entry.Offset + entry.Stored > archiveLength)
                            throw new InvalidDataException("TRE entry out of bounds");
                        Entries[name] = entry;
                    }
                }
                catch
                {
                    Stream.Dispose();
                    throw;
                }
            }

            public byte[] Read(string name)
            {
                var e = Entries[name];
                return ReadBlock(Stream, e.Offset, e.Stored, e.Compression, e.Size);
            }

            public void Dispose()
            {
                Stream.Dispose();
            }
        }

        readonly string root;
        readonly List<Archive> archives = new List<Archive>();
        readonly object gate = new object();
        public int ArchiveCount => archives.Count;

        // Same precedence as Read: the first configured archive containing a name wins.
        public IReadOnlyDictionary<string, string> Catalog()
        {
            lock (gate)
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var archive in archives)
                    foreach (var name in archive.Entries.Keys)
                        if (!result.ContainsKey(name))
                            result.Add(name, archive.Path);
                return result;
            }
        }

        public readonly Dictionary<string, string> Sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public TreAssetLoader(string directory, string config)
        {
            root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var trees = new List<Tuple<uint, string>>();
            uint max = uint.MaxValue;
            try
            {
                ReadConfig(config, new HashSet<string>(StringComparer.OrdinalIgnoreCase), trees, ref max);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tree in trees.Where(t => t.Item1 <= max).OrderByDescending(t => t.Item1))
                {
                    string path = Confined(tree.Item2);
                    if (seen.Add(path))
                        archives.Add(new Archive(path));
                }

                if (archives.Count == 0)
                    throw new InvalidDataException("No TRE archives configured");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public byte[] Read(string name)
        {
            lock (gate)
            {
                name = Normalize(name);
                string loose = Confined(name);
                if (File.Exists(loose))
                {
                    if (new FileInfo(loose).Length > Limit)
                        throw new InvalidDataException("Asset too large");
                    Sources[name] = loose;
                    return File.ReadAllBytes(loose);
                }

                foreach (var archive in archives)
                {
                    if (archive.Entries.ContainsKey(name))
                    {
                        var data = archive.Read(name);
                        Sources[name] = archive.Path;
                        return data;
                    }
                }

                throw new FileNotFoundException("Original asset not found: " + name);
            }
        }

        public string Text(string name) => System.Text.Encoding.UTF8.GetString(Read(name));
        internal static string Normalize(string name)
        {
            name = name.Replace('\\', '/').ToLowerInvariant();
            if (name.Length == 0 || name.StartsWith("/") || name.Contains(":"))
                throw new InvalidDataException("Invalid virtual asset path");
            if (name.Split('/').Any(s => s.Length == 0 || s == "." || s == ".."))
                throw new InvalidDataException("Invalid virtual asset path");
            return name;
        }

        string Confined(string name)
        {
            string full = Path.GetFullPath(Path.Combine(root, Normalize(name)));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path outside game directory");
            return full;
        }

        void ReadConfig(string name, HashSet<string> stack, List<Tuple<uint, string>> trees, ref uint max)
        {
            string path = Confined(name);
            if (stack.Count >= 32 || !stack.Add(path))
                throw new InvalidDataException("Recursive config include");
            bool shared = false;
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Split('#')[0].Trim();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith(".include"))
                {
                    ReadConfig(line.Substring(8).Trim().Trim('"'), stack, trees, ref max);
                    continue;
                }

                if (line.StartsWith("["))
                {
                    shared = line.Equals("[SharedFile]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!shared)
                    continue;
                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim().Trim('"');
                if (key.Equals("maxSearchPriority", StringComparison.OrdinalIgnoreCase))
                    max = uint.Parse(value);
                else if (key.StartsWith("searchTree_", StringComparison.OrdinalIgnoreCase))
                    trees.Add(Tuple.Create(uint.Parse(key.Substring(key.LastIndexOf('_') + 1)), value));
            }

            stack.Remove(path);
        }

        static byte[] ReadBlock(FileStream stream, long offset, int stored, uint method, int expected)
        {
            if (stored < 0 || expected < 0 || stored > Limit || expected > Limit || offset < 0 || offset > stream.Length - stored)
                throw new InvalidDataException("Invalid TRE block bounds");
            stream.Position = offset;
            var bytes = new byte[stored];
            int p = 0;
            while (p < bytes.Length)
            {
                int n = stream.Read(bytes, p, bytes.Length - p);
                if (n == 0)
                    throw new EndOfStreamException();
                p += n;
            }

            if (method == 2)
                return Zlib.Inflate(bytes, Limit, expected);
            if (method != 0)
                throw new InvalidDataException("Unsupported TRE compression method " + method);
            if (bytes.Length != expected)
                throw new InvalidDataException("TRE block size mismatch");
            return bytes;
        }

        public void Dispose()
        {
            foreach (var archive in archives)
                archive.Dispose();
            archives.Clear();
        }
    }
}
