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
        readonly object gate = new object ();
        public int ArchiveCount => archives.Count;

        // Same precedence as Read: the first listed archive containing a name wins.
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
        public TreAssetLoader(string directory, IReadOnlyDictionary<string, int> priorityOverrides = null)
        {
            root = Path.GetFullPath(directory);
            try
            {
                var archiveOrder = TreArchiveDiscovery.Discover(root, priorityOverrides);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string name in archiveOrder)
                {
                    if (!string.Equals(Path.GetExtension(name), ".tre", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Only TRE archives may be mounted: " + name);
                    string path = Confined(name);
                    if (seen.Add(path))
                        archives.Add(new Archive(path));
                }

                // Only custom archives need collision validation; standard installations avoid a second catalog index.
                var unresolved = new HashSet<string>(archives.Select(archive => Path.GetFileName(archive.Path)).Where(name => !TreArchiveDiscovery.HasKnownPriority(name) && !(priorityOverrides?.Keys.Contains(name, StringComparer.OrdinalIgnoreCase) ?? false)), StringComparer.OrdinalIgnoreCase);
                if (unresolved.Count > 0)
                {
                    var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var archive in archives)
                    {
                        string name = Path.GetFileName(archive.Path);
                        foreach (string asset in archive.Entries.Keys)
                        {
                            if (owners.TryGetValue(asset, out string other))
                            {
                                if (unresolved.Contains(name) || unresolved.Contains(other))
                                    throw new InvalidDataException("Ambiguous TRE priority: '" + name + "' and '" + other + "' both contain '" + asset + "'. Set their order in [TreArchivePriorities].");
                            }
                            else
                                owners.Add(asset, name);
                        }
                    }
                }
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
            if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path outside TRE directory");
            return full;
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
