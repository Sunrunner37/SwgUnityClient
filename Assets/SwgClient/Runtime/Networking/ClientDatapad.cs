using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Datapad views of replicated containment, waypoints, granted schematics, and the native POI catalog.</summary>
    public sealed class ClientDatapad : IDisposable
    {
        public enum Tab { Waypoints, Data, PointsOfInterest, DraftSchematics }
        public sealed class Entry
        {
            public string Key;
            public string Name;
            public string Kind;
            public string Description;
            public string PreviewPath;
            public SceneObject SceneObject;
            public Waypoint? Waypoint;
            public uint SchematicCrc;
            public string Planet;
            public SharedDraftSchematicTemplate.Attribute[] Attributes = Array.Empty<SharedDraftSchematicTemplate.Attribute>();
        }
        sealed class Poi
        {
            public string Key => "poi:" + Planet + ":" + Name;
            public Vector3 Position;
            public string Name;
            public string Description;
            public string Planet;
            public string Appearance;
        }
        readonly WorldState state;
        readonly TreRuntimeAssets assets;
        readonly SceneObjectNameResolver names;
        readonly StringTableLocalizer localizer;
        readonly Action<string, string, SwgId> send;
        readonly List<Poi> pois = new List<Poi>();
        readonly Dictionary<uint, string> planets = new Dictionary<uint, string>();
        readonly Dictionary<uint, string> planetAppearances = new Dictionary<uint, string>();
        readonly DraftSchematicCatalog schematics;
        public Task Loading { get; }
        public event Action Changed;
        public SwgId ContainerSwgId { get; private set; }
        public bool OnlyCurrentPlanet { get; set; }
        public bool SingleActiveWaypoint { get; set; }
        public int Capacity => state.SceneObjects.TryGet(ContainerSwgId, out var container) ? state.Templates.ContainerCapacity(container.TemplatePath) : 0;
        // Waypoints are a separate PLAY8 list; they do not consume the datapad container's volume.
        public int UsedCapacity => DataObjects().Sum(item => item switch
        {
            ManufactureSchematicSceneObject schematic => Math.Max(1, schematic.DataSize),
            TangibleSceneObject tangible => Math.Max(1, tangible.Volume),
            _ => 1
        });

        public ClientDatapad(WorldState state, TreRuntimeAssets assets, SceneObjectNameResolver names, StringTableLocalizer localizer, Action<string, string, SwgId> send, DraftSchematicCatalog schematics)
        {
            this.schematics = schematics;
            this.state = state;
            this.assets = assets;
            this.names = names;
            this.localizer = localizer;
            this.send = send;
            state.WaypointsChanged += Refresh;
            state.DraftSchematicsChanged += Refresh;
            state.SceneStarted += SceneStarted;
            state.SceneObjects.ObjectChanged += ObjectChanged;
            state.SceneObjects.Removed += Removed;
            Loading = LoadAsync();
            Refresh();
        }
        async Task LoadAsync()
        {
            var metadata = await assets.ReadAsync(loader =>
            {
                var poiTable = DataTableFile.Parse(loader.Read("datatables/clientpoi/clientpoi.iff"));
                var entries = poiTable.Rows.Select(row => new Poi { Name = row.Get<string>("Name"), Description = row.Get<string>("Description"), Planet = row.Get<string>("Planet"), Appearance = row.Get<string>("Appearance"), Position = new Vector3(row.Get<float>("X"), row.Get<float>("Y"), row.Get<float>("Z")) }).ToArray();
                var planetTable = DataTableFile.Parse(loader.Read("datatables/player/waypoint_planets.iff"));
                return (entries, planets: planetTable.Rows.Select(row => (name: row.Get<string>("name"), appearance: row.Get<string>("appearance"))).ToArray());
            });
            pois.AddRange(metadata.entries);
            state.SetPoiWaypoints(pois.Select(point => new Waypoint(default, point.Position, ObjectTemplateNames.TemplateCrc(point.Planet), point.Name, WaypointColor.Yellow, false, point.Key)));
            foreach (var planet in metadata.planets)
            {
                uint crc = ObjectTemplateNames.TemplateCrc(planet.name);
                planets[crc] = planet.name;
                planetAppearances[crc] = "appearance/" + planet.appearance;
            }
            Changed?.Invoke();
        }
        public string PlanetName(uint crc) => planets.TryGetValue(crc, out var name) ? localizer.Lookup("planet_n", name) : "Unknown";
        string PlanetAppearance(uint crc) => planetAppearances.TryGetValue(crc, out var path) ? path : "appearance/ui_planet_sel_default.apt";
        IEnumerable<SceneObject> DataObjects() => state.SceneObjects.ChildrenOf(ContainerSwgId).Where(item => item.TemplatePath?.StartsWith("object/waypoint/", StringComparison.Ordinal) != true);
        void SceneStarted() => Refresh();
        void Refresh()
        {
            var container = state.SceneObjects.ChildrenOf(state.PlayerSwgId).FirstOrDefault(item => item.TemplatePath?.StartsWith("object/tangible/datapad/", StringComparison.Ordinal) == true);
            ContainerSwgId = container?.SwgId ?? default;
            Changed?.Invoke();
        }
        void ObjectChanged(SceneObject item, SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment | SceneObjectChange.Inventory | SceneObjectChange.Replaced)) != 0
                && (item.SwgId == ContainerSwgId || item.ParentSwgId == ContainerSwgId || item.ParentSwgId == state.PlayerSwgId))
                Refresh();
        }
        void Removed(SceneObject item)
        {
            if (item.SwgId == ContainerSwgId || item.ParentSwgId == ContainerSwgId) Refresh();
        }
        public void Open()
        {
            Refresh();
            if (!ContainerSwgId.IsNone) send("openContainer", "", ContainerSwgId);
        }
        public IEnumerable<Entry> Entries(Tab tab)
        {
            switch (tab)
            {
                case Tab.Waypoints:
                    return state.Waypoints.Values.Where(w => !OnlyCurrentPlanet || w.PlanetCrc == state.PlanetCrc).Select(w => new Entry
                    {
                        Key = "waypoint:" + w.SwgId, Name = localizer.Localize(w.Name), Kind = "Waypoint", Waypoint = w,
                        PreviewPath = PlanetAppearance(w.PlanetCrc), Planet = PlanetName(w.PlanetCrc)
                    }).ToArray();
                case Tab.Data:
                    return DataObjects().Select(DataEntry).ToArray();
                case Tab.PointsOfInterest:
                    return pois.Where(p => ObjectTemplateNames.TemplateCrc(p.Planet) == state.PlanetCrc).Select(p => new Entry
                    {
                        Key = p.Key, Name = localizer.Localize(p.Name), Description = localizer.Localize(p.Description),
                        Waypoint = state.PoiWaypoints[p.Key], Planet = PlanetName(ObjectTemplateNames.TemplateCrc(p.Planet)),
                        PreviewPath = string.IsNullOrEmpty(p.Appearance) ? PlanetAppearance(state.PlanetCrc) : "appearance/" + p.Appearance
                    }).ToArray();
                default:
                    return state.DraftSchematics.Select(Schematic).Where(entry => entry != null).ToArray();
            }
        }
        Entry Schematic(uint crc)
        {
            var draft = schematics.Get(crc);
            return draft == null ? null : new Entry { Key = "draft:" + crc, Name = draft.Name, Kind = "Data", Description = draft.Description, PreviewPath = draft.PreviewPath, SchematicCrc = crc, Attributes = draft.Attributes };
        }
        Entry DataEntry(SceneObject item)
        {
            var draft = item is ManufactureSchematicSceneObject schematic && schematic.DraftSchematicCrc != 0 ? Schematic(schematic.DraftSchematicCrc) : null;
            return new Entry { Key = "data:" + item.SwgId, Name = names.Resolve(item), Kind = names.Resolve(new SceneObject { TemplatePath = item.TemplatePath }), Description = names.Description(item), SceneObject = item, PreviewPath = draft?.PreviewPath ?? item.TemplatePath };
        }
        public void ToggleWaypoint(Waypoint waypoint)
        {
            if (!waypoint.Active && SingleActiveWaypoint)
                foreach (var active in state.NavigationWaypoints.Where(point => point.Active).ToArray())
                    SetWaypointActive(active, false);
            SetWaypointActive(waypoint, !waypoint.Active);
        }
        void SetWaypointActive(Waypoint waypoint, bool active)
        {
            if (waypoint.PoiKey != null)
                state.SetPoiWaypointActive(waypoint.PoiKey, active);
            else if (waypoint.Active != active)
                send("setWaypointActiveStatus", "", waypoint.SwgId);
        }
        public void Dispose()
        {
            state.WaypointsChanged -= Refresh;
            state.DraftSchematicsChanged -= Refresh;
            state.SceneStarted -= SceneStarted;
            state.SceneObjects.ObjectChanged -= ObjectChanged;
            state.SceneObjects.Removed -= Removed;
        }
    }
}
