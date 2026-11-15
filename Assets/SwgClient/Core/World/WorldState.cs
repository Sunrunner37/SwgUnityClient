using Swg.Client.Core.Characters;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Crafting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Commands;
using UnityEngine;
using Swg.Client.Core.World.Terrain;

namespace Swg.Client.Core.World
{
    /// <summary>Stable session-owned state. Native world assets are populated during world entry.</summary>
    public sealed class WorldState : IDisposable
    {
        public SceneObjectRegistry SceneObjects { get; }
        public CharacterSheetData CharacterSheet { get; private set; } = new CharacterSheetData();
        public CraftingProgress Crafting { get; } = new CraftingProgress();
        public PlayerCommands Commands { get; } = new PlayerCommands();
        public SwgId PlayerSwgId { get; private set; }
        public event Action SceneStarted;
        readonly Dictionary<string, int> experience = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, int> Experience => experience;
        public event Action ExperienceChanged;
        internal void UpdateExperience(Action<Dictionary<string, int>> update)
        {
            update(experience);
            ExperienceChanged?.Invoke();
        }
        readonly List<uint> draftSchematics = new List<uint>();
        public IReadOnlyList<uint> DraftSchematics => draftSchematics;
        public event Action DraftSchematicsChanged;
        readonly Dictionary<uint, DraftSchematicDetails> draftDetails = new Dictionary<uint, DraftSchematicDetails>();
        public IReadOnlyDictionary<uint, DraftSchematicDetails> DraftDetails => draftDetails;
        public event Action<uint> DraftDetailsChanged;
        internal void UpdateDraftDetails(uint crc, DraftSchematicDetails details)
        {
            if (draftDetails.TryGetValue(crc, out var existing)) details.Experiments = existing.Experiments;
            draftDetails[crc] = details;
            DraftDetailsChanged?.Invoke(crc);
        }
        internal void UpdateDraftExperiments(uint crc, DraftSchematicDetails.ExperimentWeight[][] experiments)
        {
            if (!draftDetails.TryGetValue(crc, out var details)) draftDetails.Add(crc, details = new DraftSchematicDetails());
            details.Experiments = experiments;
            DraftDetailsChanged?.Invoke(crc);
        }
        public bool ShowWaypointIndicators { get; set; } = true;
        internal void UpdateDraftSchematics(Action<List<uint>> update)
        {
            update(draftSchematics);
            DraftSchematicsChanged?.Invoke();
        }
        readonly Dictionary<SwgId, Waypoint> waypoints = new Dictionary<SwgId, Waypoint>();
        public IReadOnlyDictionary<SwgId, Waypoint> Waypoints => waypoints;
        readonly Dictionary<string, Waypoint> poiWaypoints = new Dictionary<string, Waypoint>();
        public IReadOnlyDictionary<string, Waypoint> PoiWaypoints => poiWaypoints;
        static bool IsDatapad(SceneObject sceneObject) => sceneObject.TemplatePath?.StartsWith("object/tangible/datapad/", StringComparison.Ordinal) == true;
        public IEnumerable<MissionSceneObject> AcceptedMissions => SceneObjects.ChildrenOf(PlayerSwgId)
            .Where(IsDatapad)
            .SelectMany(item => item.Children).OfType<MissionSceneObject>();
        IEnumerable<Waypoint> MissionWaypoints => AcceptedMissions
            .Where(mission => mission.Waypoint.HasValue && !waypoints.ContainsKey(mission.Waypoint.Value.SwgId))
            .Select(mission => mission.Waypoint.Value);
        public IEnumerable<Waypoint> NavigationWaypoints => waypoints.Values.Concat(poiWaypoints.Values).Concat(MissionWaypoints);
        void MissionObjectChanged(SceneObject sceneObject, SceneObjectChange changes)
        {
            if (sceneObject is MissionSceneObject mission)
            {
                bool containmentChanged = (changes & SceneObjectChange.Containment) != 0 && mission.Waypoint.HasValue;
                bool waypointUpdated = (changes & SceneObjectChange.Waypoint) != 0
                    && AcceptedMissions.Any(item => item.SwgId == mission.SwgId);
                if (containmentChanged || waypointUpdated)
                    WaypointsChanged?.Invoke();
            }
            else if (IsDatapad(sceneObject) && (changes & SceneObjectChange.Containment) != 0)
                WaypointsChanged?.Invoke();
        }
        void MissionObjectRemoved(SceneObject sceneObject)
        {
            if (sceneObject is MissionSceneObject mission && mission.Waypoint.HasValue || IsDatapad(sceneObject))
                WaypointsChanged?.Invoke();
        }
        internal void SetPoiWaypoints(IEnumerable<Waypoint> points)
        {
            poiWaypoints.Clear();
            foreach (var point in points) poiWaypoints.Add(point.PoiKey, point);
            WaypointsChanged?.Invoke();
        }
        internal void SetPoiWaypointActive(string key, bool active)
        {
            var point = poiWaypoints[key];
            poiWaypoints[key] = new Waypoint(default, point.Position, point.PlanetCrc, point.Name, WaypointColor.Yellow, active, key);
            WaypointsChanged?.Invoke();
        }
        public event Action WaypointsChanged;
        public uint PlanetCrc => ObjectTemplateNames.TemplateCrc(Path.GetFileNameWithoutExtension(TerrainPath ?? ""));

        internal void UpdateWaypoints(Action<Dictionary<SwgId, Waypoint>> update)
        {
            update(waypoints);
            WaypointsChanged?.Invoke();
        }
        /// <summary>SWG world position mapped to the Unity render origin.</summary>
        public Vector3 RenderOrigin { get; private set; }
        public string TerrainPath { get; private set; }
        public WorldSnapshot Snapshot { get; internal set; }
        public WorldTerrain Terrain { get; internal set; }
        public ObjectTemplateCatalog Templates { get; internal set; }

        public WorldState(SceneObjectRegistry sceneObjects)
        {
            SceneObjects = sceneObjects;
            SceneObjects.ObjectChanged += MissionObjectChanged;
            SceneObjects.Removed += MissionObjectRemoved;
        }
        public void Dispose()
        {
            SceneObjects.ObjectChanged -= MissionObjectChanged;
            SceneObjects.Removed -= MissionObjectRemoved;
        }
        internal void BeginScene(SwgId playerSwgId, Vector3 renderOrigin, string terrainPath)
        {
            CharacterSheet = new CharacterSheetData();
            experience.Clear();
            draftSchematics.Clear();
            draftDetails.Clear();
            ShowWaypointIndicators = true;
            SceneObjects.Clear();
            Commands.Apply(list => list.Clear());
            PlayerSwgId = playerSwgId;
            RenderOrigin = renderOrigin;
            TerrainPath = terrainPath;
            Snapshot = null;
            Terrain = null;
            Templates = null;
            SceneStarted?.Invoke();
        }
    }
}
