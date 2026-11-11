using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Customization;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Runtime.World
{
    /// <summary>The model inputs and identity shared by entity reconciliation and asset decoding.</summary>
    public sealed class WorldAppearance
    {
        public string TemplatePath { get; }
        public CachedCharacterAppearance Character { get; }
        public string Key { get; }

        public WorldAppearance(string templatePath, CachedCharacterAppearance character = null)
        {
            TemplatePath = templatePath;
            Character = character;
            var key = new StringBuilder(templatePath);
            if (character != null)
            {
                key.Append(':').Append(Convert.ToBase64String(character.Customization.Encoded));
                foreach (var wearable in character.Wearables)
                    key.Append('|').Append(wearable.TemplatePath).Append(':').Append(wearable.ArrangementHint).Append(':').Append(Convert.ToBase64String(wearable.Customization.Encoded));
            }

            Key = key.ToString();
        }

        public static Dictionary<SwgId, WorldAppearance> Capture(WorldState data, GameWorldSnapshot snapshot)
        {
            var objects = snapshot.Objects.ToDictionary(state => state.SwgId);
            var children = snapshot.Objects.ToLookup(state => state.ParentSwgId);
            var appearances = new Dictionary<SwgId, WorldAppearance>();
            foreach (var state in snapshot.Objects)
            {
                var appearance = Describe(data, snapshot, state, objects, children);
                if (appearance != null)
                    appearances.Add(state.SwgId, appearance);
            }

            return appearances;
        }

        public static WorldAppearance Describe(WorldState data, GameWorldSnapshot snapshot, SceneObject state)
        {
            return Describe(data, snapshot, state, snapshot.Objects.ToDictionary(item => item.SwgId), snapshot.Objects.ToLookup(item => item.ParentSwgId));
        }

        public static WorldAppearance Describe(WorldState data, SceneObject state)
        {
            var parent = state.Parent;
            return Describe(data, state, parent, state.Children, data.TerrainPath);
        }

        static WorldAppearance Describe(WorldState data, GameWorldSnapshot snapshot, SceneObject state, IReadOnlyDictionary<SwgId, SceneObject> objects, ILookup<SwgId, SceneObject> children)
        {
            objects.TryGetValue(state.ParentSwgId, out var parent);
            return Describe(data, state, parent, children[state.SwgId], snapshot.StartScene?.TerrainPath);
        }

        static WorldAppearance Describe(WorldState data, SceneObject state, SceneObject parent, IEnumerable<SceneObject> children, string terrainPath)
        {
            if (state is not SpatialSceneObject)
                return null;
            if (data.Snapshot != null && data.Snapshot.ById.ContainsKey(state.SwgId))
                return null;
            string path = data.Templates.Resolve(state.TemplateCrc) ?? state.TemplatePath;
            if (string.IsNullOrEmpty(path) || IsCell(path))
                return null;
            if (state.ParentSwgId != 0)
            {
                bool cell = parent != null && IsCell(data.Templates.Resolve(parent.TemplateCrc) ?? parent.TemplatePath);
                bool staticCell = data.Snapshot != null && data.Snapshot.ById.TryGetValue(state.ParentSwgId, out var staticParent) && IsCell(staticParent.TemplatePath);
                if (!cell && !staticCell)
                    return null;
            }

            if (state is not CreatureSceneObject creature)
                return new WorldAppearance(path);
            var wearables = new List<CachedWearableAppearance>();
            var equippedObjects = children.OfType<TangibleSceneObject>().ToDictionary(child => child.SwgId);
            foreach (var item in creature.Equipment.OrderBy(item => item.SwgId))
            {
                string template = data.Templates.Resolve(item.TemplateCrc);
                if (template == null)
                    continue;
                var customization = equippedObjects.TryGetValue(item.SwgId, out var child) && child.Customization.Length > 0
                    ? child.Customization : item.Customization;
                wearables.Add(new CachedWearableAppearance { TemplatePath = template, Customization = PackedCustomization.Decode(customization), ArrangementHint = item.Arrangement });
            }

            var character = new CachedCharacterAppearance
            {
                TemplatePath = path,
                Customization = PackedCustomization.Decode(creature.Customization),
                Wearables = wearables,
                Planet = Path.GetFileNameWithoutExtension(terrainPath ?? ""),
                SourcePath = "network",
                FromCache = false
            };
            return new WorldAppearance(path, character);
        }

        internal static bool IsCell(string path) => path != null && (path.Contains("/cell/") || path.EndsWith("/shared_cell.iff", StringComparison.OrdinalIgnoreCase));
    }
}
