using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Shared draft metadata and server-requested ingredients/weights for crafting and datapad views.</summary>
    public sealed class DraftSchematicCatalog : IDisposable
    {
        public sealed class Draft
        {
            public string Name;
            public string Description;
            public string PreviewPath;
            public uint SchematicCrc;
            public SharedDraftSchematicTemplate.Attribute[] Attributes;
        }
        readonly WorldState state;
        readonly TreRuntimeAssets assets;
        readonly SceneObjectNameResolver names;
        readonly StringTableLocalizer localizer;
        readonly Action<string, string, SwgId> send;
        readonly Dictionary<uint, Draft> schematics = new Dictionary<uint, Draft>();
        readonly HashSet<uint> requested = new HashSet<uint>();
        public DraftSchematicCatalog(WorldState state, TreRuntimeAssets assets, SceneObjectNameResolver names, StringTableLocalizer localizer, Action<string, string, SwgId> send)
        {
            this.state = state;
            this.assets = assets;
            this.names = names;
            this.localizer = localizer;
            this.send = send;
            state.SceneStarted += ResetRequests;
        }
        void ResetRequests() => requested.Clear();
        public Draft Get(uint crc)
        {
            if (schematics.TryGetValue(crc, out var cached)) return cached;
            string path = state.Templates.Resolve(crc);
            if (path == null) return null;
            byte[] bytes = assets.Loader.Read(path);
            string crafted = SharedDraftSchematicTemplate.CraftedTemplate(bytes);
            var item = new SceneObject { TemplatePath = crafted ?? path };
            cached = new Draft { Name = names.Resolve(item), Description = names.Description(item), PreviewPath = crafted ?? path, SchematicCrc = crc, Attributes = SharedDraftSchematicTemplate.Attributes(bytes) };
            schematics.Add(crc, cached);
            return cached;
        }
        public string CraftingLabel(string reference)
        {
            string label = localizer.Localize(reference);
            if (!label.StartsWith("@", StringComparison.Ordinal)) return label;
            string key = reference.Substring(reference.IndexOf(':') + 1);
            label = localizer.Lookup("obj_attr_n", key);
            if (!label.StartsWith("@", StringComparison.Ordinal)) return label;
            if (key.Length > 3 && key.StartsWith("exp", StringComparison.OrdinalIgnoreCase) && (char.IsUpper(key[3]) || key[3] == '_')) key = "experimental_" + key.Substring(3).TrimStart('_');
            label = Regex.Replace(key.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2");
            return label.Length == 0 ? "" : char.ToUpperInvariant(label[0]) + label.Substring(1);
        }
        public void RequestDetails(uint crc)
        {
            if (crc != 0 && requested.Add(crc))
            {
                string arguments = crc + " " + crc;
                send("requestDraftSlotsBatch", arguments, default);
                send("requestResourceWeightsBatch", crc.ToString(), default);
            }
        }
        public bool TryGetDetails(uint crc, out DraftSchematicDetails details) => state.DraftDetails.TryGetValue(crc, out details);
        public IReadOnlyList<DraftSchematicDetails.ExperimentGroup> GetExperiments(uint crc) => TryGetDetails(crc, out var details)
            ? details.GroupExperiments(Get(crc).Attributes) : Array.Empty<DraftSchematicDetails.ExperimentGroup>();

        public void Dispose() => state.SceneStarted -= ResetRequests;
    }
}
