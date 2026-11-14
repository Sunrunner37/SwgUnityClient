using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Customization;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.World
{
    /// <summary>The appearance inputs and identity shared by entity reconciliation and asset decoding.</summary>
    public sealed class AppearanceRequest
    {
        public string TemplatePath { get; }
        public CharacterAppearance Character { get; }
        public string Key { get; }

        public AppearanceRequest(string templatePath, CharacterAppearance character = null)
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

        public static Dictionary<SwgId, AppearanceRequest> Capture(WorldState state, GameWorldSnapshot snapshot)
        {
            var objects = snapshot.Objects.ToDictionary(sceneObject => sceneObject.SwgId);
            var children = snapshot.Objects.ToLookup(sceneObject => sceneObject.ParentSwgId);
            var appearances = new Dictionary<SwgId, AppearanceRequest>();
            foreach (var sceneObject in snapshot.Objects)
            {
                var appearance = Describe(state, snapshot, sceneObject, objects, children);
                if (appearance != null)
                    appearances.Add(sceneObject.SwgId, appearance);
            }

            return appearances;
        }

        public static AppearanceRequest Describe(WorldState state, GameWorldSnapshot snapshot, SceneObject sceneObject)
        {
            return Describe(state, snapshot, sceneObject, snapshot.Objects.ToDictionary(item => item.SwgId), snapshot.Objects.ToLookup(item => item.ParentSwgId));
        }

        public static AppearanceRequest Describe(WorldState state, SceneObject sceneObject)
        {
            var parent = sceneObject.Parent;
            return Describe(state, sceneObject, parent, sceneObject.Children);
        }

        static AppearanceRequest Describe(WorldState state, GameWorldSnapshot snapshot, SceneObject sceneObject, IReadOnlyDictionary<SwgId, SceneObject> objects, ILookup<SwgId, SceneObject> children)
        {
            objects.TryGetValue(sceneObject.ParentSwgId, out var parent);
            return Describe(state, sceneObject, parent, children[sceneObject.SwgId]);
        }

        static AppearanceRequest Describe(WorldState state, SceneObject sceneObject, SceneObject parent, IEnumerable<SceneObject> children)
        {
            if (sceneObject is not SpatialSceneObject)
                return null;

            if (state.Snapshot != null && state.Snapshot.ById.ContainsKey(sceneObject.SwgId))
                return null;

            string path = state.Templates.Resolve(sceneObject.TemplateCrc) ?? sceneObject.TemplatePath;

            if (string.IsNullOrEmpty(path) || IsCell(path))
                return null;

            if (sceneObject.ParentSwgId != 0)
            {
                bool cell = parent != null && IsCell(state.Templates.Resolve(parent.TemplateCrc) ?? parent.TemplatePath);
                bool staticCell = state.Snapshot != null && state.Snapshot.ById.TryGetValue(sceneObject.ParentSwgId, out var staticParent) && IsCell(staticParent.TemplatePath);
                bool riding = sceneObject is CreatureSceneObject && parent is CreatureSceneObject;
                if (!cell && !staticCell && !riding)
                    return null;
            }

            if (sceneObject is not CreatureSceneObject creature)
                return new AppearanceRequest(path);

            var wearables = new List<WearableAppearance>();
            var equippedObjects = children.OfType<TangibleSceneObject>().ToDictionary(child => child.SwgId);

            foreach (var item in creature.Equipment.OrderBy(item => item.SwgId))
            {
                string template = state.Templates.Resolve(item.TemplateCrc);
                if (template == null)
                    continue;
                var customization = equippedObjects.TryGetValue(item.SwgId, out var child) && child.Customization.Length > 0 ? child.Customization : item.Customization;
                wearables.Add(new WearableAppearance { TemplatePath = template, Customization = PackedCustomization.Decode(customization), ArrangementHint = item.Arrangement });
            }

            var character = new CharacterAppearance
            {
                TemplatePath = path,
                Customization = PackedCustomization.Decode(creature.Customization),
                Wearables = wearables,
            };

            return new AppearanceRequest(path, character);
        }

        internal static bool IsCell(string path) => path != null && (path.Contains("/cell/") || path.EndsWith("/shared_cell.iff", StringComparison.OrdinalIgnoreCase));
    }
}
