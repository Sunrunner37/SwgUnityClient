using Swg.Client.Core;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.World
{
    /// <summary>Original WSNP/0001 placement data, including parent/cell transforms.</summary>
    public sealed class WorldSnapshot
    {
        public IReadOnlyList<SnapshotEntry> Entries { get; }
        public IReadOnlyDictionary<SwgId, SnapshotEntry> ById { get; }

        WorldSnapshot(List<SnapshotEntry> entries)
        {
            Entries = entries;
            var lookup = new Dictionary<SwgId, SnapshotEntry>();
            foreach (var entry in entries)
                if (!lookup.TryAdd(entry.SwgId, entry))
                    throw new InvalidDataException("Duplicate snapshot object ID");
            ById = lookup;
            var states = new Dictionary<SwgId, byte>();
            foreach (var entry in entries)
                Resolve(entry, lookup, states, 0);
        }

        public static WorldSnapshot Load(TreAssetLoader loader, string planetOrSnapshotPath)
        {
            planetOrSnapshotPath = planetOrSnapshotPath.Replace('\\', '/');
            string path = planetOrSnapshotPath.Contains('/') ? planetOrSnapshotPath : "snapshot/" + planetOrSnapshotPath + ".ws";
            return Parse(loader.Read(path));
        }

        public static WorldSnapshot Parse(byte[] bytes)
        {
            var form = IffNode.Form(IffNode.Parse(bytes), "WSNP") ?? throw new InvalidDataException("Expected world snapshot");
            var version = form.Children.SingleOrDefault(n => n.Kind == "0001") ?? throw new InvalidDataException("Unsupported snapshot version");
            var reader = new ByteReader(version.Chunk("OTNL"));
            int count = checked((int)reader.U32());
            if (count > 100000)
                throw new InvalidDataException("Snapshot template count limit");
            var names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = TreAssetLoader.Normalize(reader.CString());
            var nodes = version.Children.SingleOrDefault(n => n.Kind == "NODS") ?? throw new InvalidDataException("Missing snapshot nodes");
            var entries = new List<SnapshotEntry>();
            ReadNodes(nodes.Children, names, entries, 0);
            return new WorldSnapshot(entries);
        }

        static void ReadNodes(IEnumerable<IffNode> nodes, string[] names, List<SnapshotEntry> entries, int depth)
        {
            if (depth > 24 || entries.Count > 1000000)
                throw new InvalidDataException("Snapshot hierarchy limit");
            foreach (var node in nodes.Where(n => n.Kind == "NODE"))
            {
                var version = node.Children.SingleOrDefault(n => n.Kind == "0000") ?? throw new InvalidDataException("Unsupported snapshot node version");
                var reader = new ByteReader(version.Chunk("DATA"));
                var entry = new SnapshotEntry
                {
                    SwgId = reader.U32(),
                    ParentSwgId = reader.U32()
                };
                uint name = reader.U32();
                if (name >= names.Length)
                    throw new InvalidDataException("Invalid snapshot template index");
                entry.TemplatePath = names[name];
                entry.CellSwgId = reader.U32();
                // WSNP stores W first; the shared quaternion uses X, Y, Z, W.
                float w = reader.Float();
                entry.Rotation = new Quaternion(reader.Float(), reader.Float(), reader.Float(), w).normalized;
                entry.Position = new Vector3(reader.Float(), reader.Float(), reader.Float());
                entry.NativeValue = reader.Float();
                entry.Reserved = reader.U32();
                if (reader.Remaining != 0)
                    throw new InvalidDataException("Unexpected snapshot node data");
                entries.Add(entry);
                ReadNodes(version.Children, names, entries, depth + 1);
            }
        }

        static void Resolve(SnapshotEntry entry, Dictionary<SwgId, SnapshotEntry> lookup, Dictionary<SwgId, byte> states, int depth)
        {
            if (states.TryGetValue(entry.SwgId, out byte state))
            {
                if (state == 1)
                    throw new InvalidDataException("Snapshot parent cycle");
                return;
            }

            if (depth > 24)
                throw new InvalidDataException("Snapshot parent depth limit");
            states[entry.SwgId] = 1;
            if (entry.ParentSwgId != 0 && lookup.TryGetValue(entry.ParentSwgId, out var parent))
            {
                Resolve(parent, lookup, states, depth + 1);
                var p = (parent.WorldRotation * entry.Position);
                entry.WorldPosition = parent.WorldPosition + p;
                entry.WorldRotation = (parent.WorldRotation * entry.Rotation).normalized;
            }
            else
            {
                entry.WorldPosition = entry.Position;
                entry.WorldRotation = entry.Rotation;
            }

            states[entry.SwgId] = 2;
        }

        /// <summary>Returns local placements plus a conservative padding for large building exteriors.</summary>
        public IEnumerable<SnapshotEntry> Query(Vector3 center, float radius, float boundsPadding = 128)
        {
            if (radius < 0 || boundsPadding < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));
            float distance = radius + boundsPadding;
            float squared = distance * distance;
            return Entries.Where(e =>
            {
                float x = e.WorldPosition.x - center.x;
                float z = e.WorldPosition.z - center.z;
                return x * x + z * z <= squared;
            });
        }
    }
}
