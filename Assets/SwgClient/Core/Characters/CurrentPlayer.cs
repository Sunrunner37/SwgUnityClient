using System.Linq;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Collections.Generic;
using Swg.Client.Core.Commands;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Core.Characters
{
    /// <summary>Local player identity, owner-only progress, missions, and navigation. The registry owns the replicated creature.</summary>
    public sealed class CurrentPlayer : IDisposable
    {
        readonly SceneObjectRegistry sceneObjects;
        public CreatureSceneObject Creature => sceneObjects.TryGet<CreatureSceneObject>(SwgId, out var creature) ? creature : null;
        public CurrentPlayer(SceneObjectRegistry sceneObjects)
        {
            this.sceneObjects = sceneObjects;
            sceneObjects.ObjectChanged += MissionObjectChanged;
            sceneObjects.Removed += MissionObjectRemoved;
        }
        public void Dispose()
        {
            sceneObjects.ObjectChanged -= MissionObjectChanged;
            sceneObjects.Removed -= MissionObjectRemoved;
        }
        public CharacterSheetData CharacterSheet { get; private set; } = new CharacterSheetData();
        public WeaponRanges WeaponRanges { get; internal set; }
        public PlayerCommands Commands { get; } = new PlayerCommands();
        public SwgId SwgId { get; private set; }
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
        public event Action WaypointsChanged;
        internal void UpdateWaypoints(Action<Dictionary<SwgId, Waypoint>> update)
        {
            update(waypoints);
            WaypointsChanged?.Invoke();
        }
        readonly Dictionary<string, Waypoint> poiWaypoints = new Dictionary<string, Waypoint>();
        public IReadOnlyDictionary<string, Waypoint> PoiWaypoints => poiWaypoints;
        static bool IsDatapad(SceneObject sceneObject) => sceneObject.TemplatePath?.StartsWith("object/tangible/datapad/", StringComparison.Ordinal) == true;
        public IEnumerable<MissionSceneObject> AcceptedMissions => sceneObjects.ChildrenOf(SwgId)
            .Where(IsDatapad)
            .SelectMany(item => item.Children).OfType<MissionSceneObject>();
        IEnumerable<Waypoint> MissionWaypoints => AcceptedMissions
            .Where(mission => mission.Waypoint.HasValue && !Waypoints.ContainsKey(mission.Waypoint.Value.SwgId))
            .Select(mission => mission.Waypoint.Value);
        public IEnumerable<Waypoint> NavigationWaypoints => Waypoints.Values.Concat(poiWaypoints.Values).Concat(MissionWaypoints);
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
        internal void BeginScene(SwgId swgId)
        {
            CharacterSheet = new CharacterSheetData();
            WeaponRanges = default;
            experience.Clear();
            draftSchematics.Clear();
            draftDetails.Clear();
            ShowWaypointIndicators = true;
            Commands.Apply(list => list.Clear());
            SwgId = swgId;
        }
    }
}
