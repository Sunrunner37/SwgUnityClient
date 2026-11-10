using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Swg.Client.Core.Assets.Tre
{
    /// <summary>Discovers TREs using the pre-CU patch/SKU precedence, independent of file timestamps.</summary>
    public static class TreArchiveDiscovery
    {
        static readonly Regex patch = new Regex(@"^patch_(sku1_)?(\d{1,3})(?:_(\d{1,3}))?\.tre$", RegexOptions.IgnoreCase);
        static readonly Regex hotfix = new Regex(@"^hotfix_(sku1_)?(\d{1,3})_(\d{1,3})_(\d{1,3})\.tre$", RegexOptions.IgnoreCase);
        static readonly Regex data = new Regex(@"^data_(other|static_mesh|texture|skeletal_mesh|animation|sample|music|sku1)_(\d{1,3})\.tre$", RegexOptions.IgnoreCase);
        static readonly string[] baseGroups =
        {
            "music",
            "sample",
            "animation",
            "skeletal_mesh",
            "texture",
            "static_mesh",
            "other"
        };
        public static IReadOnlyList<string> Discover(string directory, IReadOnlyDictionary<string, int> priorityOverrides = null)
        {
            var overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (priorityOverrides != null)
                foreach (var pair in priorityOverrides)
                    overrides.Add(pair.Key, pair.Value);
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).Where(path => Path.GetExtension(path).Equals(".tre", StringComparison.OrdinalIgnoreCase)).Select(Path.GetFileName).ToArray();
            if (files.Length == 0)
                throw new InvalidDataException("No TRE archives found in " + directory);
            foreach (var name in overrides.Keys)
                if (!files.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException("TRE priority override names a missing archive: " + name);
            return files.Select(name =>
            {
                bool known = TryPriority(name, out int priority, out int tie);
                if (overrides.TryGetValue(name, out int configured))
                    priority = configured;
                else if (!known)
                    Debug.LogWarning("Unknown TRE archive '" + name + "': only nonconflicting assets can be mounted without a [TreArchivePriorities] override.");
                return new
                {
                    Name = name,
                    Priority = priority,
                    Tie = tie
                };
            }).OrderByDescending(item => item.Priority).ThenByDescending(item => item.Tie).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Select(item => item.Name).ToArray();
        }

        internal static bool HasKnownPriority(string name) => TryPriority(name, out _, out _);
        static bool TryPriority(string name, out int priority, out int tie)
        {
            priority = -1;
            tie = 0;
            if (name.Equals("default_patch.tre", StringComparison.OrdinalIgnoreCase))
            {
                priority = 1000000;
                return true;
            }

            if (name.Equals("bottom.tre", StringComparison.OrdinalIgnoreCase))
            {
                priority = 0;
                return true;
            }

            var match = patch.Match(name);
            if (match.Success)
            {
                bool sku = match.Groups[1].Success;
                int version = int.Parse(match.Groups[2].Value);
                int part = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                if (sku && version < 12)
                    return false;
                // Patch 11 was interleaved with the final SKU1 data releases.
                priority = version == 11 ? 1400 + Math.Max(0, part - 1) * 200 : PatchPriority(version);
                if (sku)
                    priority += 100;
                tie = part;
                return true;
            }

            match = hotfix.Match(name);
            if (match.Success)
            {
                bool sku = match.Groups[1].Success;
                int version = int.Parse(match.Groups[2].Value);
                if (sku && version < 12)
                    return false;
                priority = PatchPriority(version) + (sku ? 100 : 0) + 50;
                tie = int.Parse(match.Groups[3].Value) * 1000 + int.Parse(match.Groups[4].Value);
                return true;
            }

            match = data.Match(name);
            if (!match.Success)
                return false;
            string group = match.Groups[1].Value.ToLowerInvariant();
            int index = int.Parse(match.Groups[2].Value);
            if (group == "sku1")
            {
                if (index > 7)
                    return false;
                priority = index <= 5 ? 1300 : index == 6 ? 1500 : 1700;
                tie = index;
            }
            else
            {
                priority = 100;
                tie = Array.IndexOf(baseGroups, group) * 10000 + index;
            }

            return true;
        }

        static int PatchPriority(int version) => version <= 10 ? (version + 2) * 100 : version == 11 ? 1800 : (version * 2 - 5) * 100;
    }
}
