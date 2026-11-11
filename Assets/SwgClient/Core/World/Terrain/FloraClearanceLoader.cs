using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Resolves client-data flora clearances through native shared-template inheritance.</summary>
    public sealed class FloraClearanceLoader
    {
        readonly ConcurrentDictionary<string, FloraClearance[]> cache = new ConcurrentDictionary<string, FloraClearance[]>();
        public FloraClearance[] GetCached(string template) => template != null && cache.TryGetValue(template, out var circles) ? circles : Array.Empty<FloraClearance>();
        public FloraClearance[] Load(TreAssetLoader assets, string template)
        {
            if (!template.StartsWith("object/", StringComparison.Ordinal))
                return Array.Empty<FloraClearance>();
            return Read(assets, template, new HashSet<string>());
        }

        FloraClearance[] Read(TreAssetLoader assets, string path, HashSet<string> visited)
        {
            if (cache.TryGetValue(path, out var cached))
                return cached;
            if (!visited.Add(path))
                throw new InvalidDataException("Object-template inheritance cycle: " + path);
            var result = new List<FloraClearance>();
            try
            {
                var bytes = assets.Read(path);
                var nodes = IffNode.Parse(bytes);
                var inherited = Inherited(nodes).Distinct().SelectMany(parent => Read(assets, parent, visited)).ToArray();
                float? radius = ClearCollidableRadius(nodes);
                if (radius.HasValue)
                {
                    if (radius.Value > 0)
                        result.Add(new FloraClearance(Vector3.zero, radius.Value, true));
                }
                else
                    result.AddRange(inherited.Where(circle => circle.Collidable));
                var files = IffNode.References(bytes, "clientdata/", ".cdf").Distinct().ToArray();
                if (files.Length > 0)
                {
                    foreach (var file in files)
                        result.AddRange(FloraClearance.FromClientData(assets.Read(file)));
                }
                else
                    result.AddRange(inherited.Where(circle => !circle.Collidable));
            }
            catch (FileNotFoundException error)
            {
                Debug.LogWarning(error.Message);
            }

            visited.Remove(path);
            return cache[path] = result.ToArray();
        }

        // FloatParam: data type byte, delta type byte, then the value. NONE inherits.
        static float? ClearCollidableRadius(IEnumerable<IffNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Tag == "XXXX")
                {
                    var reader = new ByteReader(node.Data);
                    if (reader.CString() == "clearFloraRadius")
                    {
                        byte type = reader.Byte();
                        reader.Byte();
                        if (type == 1)
                            return reader.Float();
                        if (type != 0)
                            throw new InvalidDataException("clearFloraRadius must be a single FloatParam");
                    }
                }

                var value = ClearCollidableRadius(node.Children);
                if (value.HasValue)
                    return value;
            }

            return null;
        }

        static IEnumerable<string> Inherited(IEnumerable<IffNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Kind == "DERV" || node.Tag == "DERV")
                    foreach (var path in IffNode.References(node.Data, "object/", ".iff"))
                        yield return path;
                foreach (var path in Inherited(node.Children))
                    yield return path;
            }
        }
    }
}
