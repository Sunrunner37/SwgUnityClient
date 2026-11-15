using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Runtime.UI.Theming;
using Swg.Client.Runtime.World.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Datapad
{
    public sealed class DatapadWindow : MonoBehaviour, IUiPrefabSource
    {
        public TMP_Text title;
        public TMP_Text selectedName;
        public TMP_Text selectedKind;
        public ExamineAttributes details;
        public ExaminePreview preview;
        public GridLayoutGroup grid;
        public ContainerItem itemPrefab;
        public ItemPreviewAtlas previews;
        public CapacityBar capacityBar;
        public TMP_Text capacity;
        public GameObject capacityPanel;
        public GameObject waypointOptions;
        public Toggle[] tabs;
        public Toggle onlyCurrentPlanet;
        public Toggle singleActive;
        public Toggle indicators;
        public UiTheme theme;
        public IEnumerable<GameObject> UiPrefabs { get { yield return itemPrefab.gameObject; } }
        public ClientDatapad.Tab CurrentTab { get; private set; }
        public bool Ready { get; private set; }
        public int ItemCount => entries.Length;
        readonly List<ContainerItem> cells = new List<ContainerItem>();
        readonly List<UnityAction<bool>> tabListeners = new List<UnityAction<bool>>();
        ItemPreviewCache cache;
        string[] renderedKeys = Array.Empty<string>();
        readonly Dictionary<ClientDatapad.Tab, string> selections = new Dictionary<ClientDatapad.Tab, string>();
        ClientDatapad.Entry[] entries = Array.Empty<ClientDatapad.Entry>();
        ClientDatapad datapad;
        DraftSchematicCatalog schematics;
        ClientObjectInteractions interactions;
        RadialContextMenu contextMenu;
        WorldState state;
        WorldContentLoader loader;
        StringTableLocalizer localizer;
        Coroutine loading;
        bool dirty;
        ScrollRect scroll;
        int firstVisible = -1;
        int visibleCount;
        string selected;
        void Awake()
        {
            contextMenu = GetComponentInParent<Canvas>().GetComponentInChildren<RadialContextMenu>(true);
            scroll = grid.GetComponentInParent<ScrollRect>(true);
            datapad = GameServices.Resolve<ClientDatapad>();
            schematics = GameServices.Resolve<DraftSchematicCatalog>();
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            state = GameServices.Resolve<WorldState>();
            loader = GameServices.Resolve<WorldContentLoader>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
            cache = GameServices.Resolve<ItemPreviewCache>();
        }
        void OnEnable()
        {
            datapad.Changed += Changed;
            state.DraftDetailsChanged += DetailsChanged;
            theme.Changed += RefreshDetails;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                UnityAction<bool> listener = on => { if (on) SelectTab(index); };
                tabs[i].onValueChanged.AddListener(listener);
                tabListeners.Add(listener);
            }
            onlyCurrentPlanet.SetIsOnWithoutNotify(datapad.OnlyCurrentPlanet);
            singleActive.SetIsOnWithoutNotify(datapad.SingleActiveWaypoint);
            indicators.SetIsOnWithoutNotify(state.ShowWaypointIndicators);
            onlyCurrentPlanet.onValueChanged.AddListener(FilterPlanet);
            singleActive.onValueChanged.AddListener(SetSingleActive);
            indicators.onValueChanged.AddListener(SetIndicators);
            datapad.Open();
            dirty = true;
        }
        void OnDisable()
        {
            datapad.Changed -= Changed;
            state.DraftDetailsChanged -= DetailsChanged;
            theme.Changed -= RefreshDetails;
            for (int i = 0; i < tabs.Length; i++) tabs[i].onValueChanged.RemoveListener(tabListeners[i]);
            tabListeners.Clear();
            onlyCurrentPlanet.onValueChanged.RemoveListener(FilterPlanet);
            singleActive.onValueChanged.RemoveListener(SetSingleActive);
            indicators.onValueChanged.RemoveListener(SetIndicators);
            StopAllCoroutines();
            loading = null;
            Ready = false;
            preview.Clear();
        }
        void Changed() => dirty = true;
        void DetailsChanged(uint crc)
        {
            if (entries.Any(entry => entry.Key == selected && entry.SchematicCrc == crc)) RefreshDetails();
        }
        void FilterPlanet(bool value) { datapad.OnlyCurrentPlanet = value; dirty = true; }
        void SetSingleActive(bool value) => datapad.SingleActiveWaypoint = value;
        void SetIndicators(bool value) => state.ShowWaypointIndicators = value;
        public void SelectTab(int index)
        {
            CurrentTab = (ClientDatapad.Tab)index;
            for (int i = 0; i < tabs.Length; i++) tabs[i].SetIsOnWithoutNotify(i == index);
            dirty = true;
        }
        void LateUpdate()
        {
            if (dirty)
            {
                dirty = false;
                if (loading != null) StopCoroutine(loading);
                loading = StartCoroutine(Refresh());
            }
            else if (Ready && VisibleRange() != (firstVisible, visibleCount))
                loading = StartCoroutine(RenderVisible());
        }
        (int First, int Count) VisibleRange()
        {
            float rowHeight = grid.cellSize.y + grid.spacing.y;
            float width = ((RectTransform)grid.transform).rect.width - grid.padding.horizontal;
            int columns = Mathf.Max(1, Mathf.FloorToInt((width + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
            int first = Mathf.Max(0, Mathf.FloorToInt(scroll.content.anchoredPosition.y / rowHeight)) * columns;
            int count = (Mathf.CeilToInt(scroll.viewport.rect.height / rowHeight) + 1) * columns;
            first = Mathf.Min(first, entries.Length);
            return (first, Mathf.Min(count, entries.Length - first));
        }
        AppearanceRequest Describe(ClientDatapad.Entry entry) => entry.SceneObject != null && entry.PreviewPath == entry.SceneObject.TemplatePath
            ? loader.DescribeAppearance(entry.SceneObject) : new AppearanceRequest(entry.PreviewPath);
        void ClearCells()
        {
            foreach (var cell in cells) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
            cells.Clear();
            previews.Clear();
            renderedKeys = Array.Empty<string>();
            firstVisible = -1;
            visibleCount = 0;
        }
        IEnumerator Refresh()
        {
            Ready = false;
            yield return new WaitUntil(() => datapad.Loading.IsCompleted);
            if (datapad.Loading.IsFaulted) { Debug.LogException(datapad.Loading.Exception.GetBaseException(), this); yield break; }
            var updated = datapad.Entries(CurrentTab).OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
            bool membershipChanged = cells.Count != updated.Length || !entries.Select(entry => entry.Key).SequenceEqual(updated.Select(entry => entry.Key));
            if (membershipChanged) ClearCells();
            entries = updated;
            waypointOptions.SetActive(CurrentTab == ClientDatapad.Tab.Waypoints);
            capacityPanel.SetActive(CurrentTab == ClientDatapad.Tab.Waypoints || CurrentTab == ClientDatapad.Tab.Data);
            int used = datapad.UsedCapacity;
            int limit = datapad.Capacity;
            capacity.text = "Capacity                  " + (limit > 0 ? Mathf.RoundToInt(used * 100f / limit) + "% (" + used + "/" + limit + ")" : used.ToString());
            capacityBar.Fill = limit > 0 ? used / (float)limit : 0;
            if (membershipChanged)
                foreach (var entry in entries)
                {
                    var cell = Instantiate(itemPrefab, grid.transform);
                    string key = entry.Key;
                    cell.Bind(entry.SceneObject?.SwgId ?? default, () => Select(key), () => ActivateEntry(key), entry.Waypoint.HasValue || entry.SceneObject != null ? position => OpenContextMenu(key, position) : null);
                    cell.image.color = Color.clear;
                    cells.Add(cell);
                }
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var cell = cells[i];
                cell.CanActivate = entry.Waypoint.HasValue || entry.SceneObject != null && interactions.HasDefaultAction(entry.SceneObject.SwgId);
                cell.itemName.text = entry.Name;
                cell.quantity.text = "";
                cell.equipped.SetActive(false);
                if (cell.activeIndicator != null) cell.activeIndicator.SetActive(entry.Waypoint?.Active == true || entry.SceneObject != null && interactions.IsActiveControlDevice(entry.SceneObject.SwgId));
            }
            if (!selections.TryGetValue(CurrentTab, out selected) || !entries.Any(entry => entry.Key == selected)) selected = entries.FirstOrDefault()?.Key;
            for (int i = 0; i < cells.Count; i++) cells[i].selection.SetActive(entries[i].Key == selected);
            var selectedEntry = entries.FirstOrDefault(entry => entry.Key == selected);
            if (selectedEntry != null) schematics.RequestDetails(selectedEntry.SchematicCrc);
            RefreshDetails();
            yield return null; // Let Unity's layout groups establish the viewport and grid width.
            yield return RenderVisible();
            loading = null;
        }
        IEnumerator RenderVisible()
        {
            Ready = false;
            var range = VisibleRange();
            var requests = entries.Skip(range.First).Take(range.Count).Select(Describe).ToArray();
            var keys = requests.Select(request => request.Key).ToArray();
            if (range == (firstVisible, visibleCount) && renderedKeys.SequenceEqual(keys))
            {
                RefreshPreview();
                Ready = true;
                yield break;
            }
            // Only prepare the visible rows; the shared cache owns their resources.
            yield return cache.Prepare(requests);
            for (int i = Mathf.Max(0, firstVisible); i < firstVisible + visibleCount && i < cells.Count; i++)
            {
                cells[i].image.color = Color.clear;
                cells[i].image.texture = null;
            }
            (firstVisible, visibleCount) = range;
            previews.Begin(visibleCount);
            renderedKeys = keys;
            for (int i = 0; i < visibleCount; i++)
                if (cache.TryGet(requests[i].Key, out var result))
                {
                    var cell = cells[firstVisible + i];
                    previews.Show(i, result.Appearance, result.Resources, cell);
                    cell.image.color = Color.white;
                }
            RefreshPreview();
            previews.RenderNow();
            Ready = true;
        }
        void OpenContextMenu(string key, Vector2 position)
        {
            var entry = entries.FirstOrDefault(item => item.Key == key);
            if (entry == null) return;
            Select(key);
            if (entry.SceneObject != null) { contextMenu.Open(entry.SceneObject.SwgId, position); return; }
            if (entry.Waypoint is not Waypoint waypoint) return;
            contextMenu.OpenLocal(position, new[] { new RadialMenuEntry(1, 0, 0, 0, waypoint.Active ? "Deactivate" : "Activate") }, _ => ActivateEntry(key));
        }
        void ActivateEntry(string key)
        {
            var entry = entries.FirstOrDefault(item => item.Key == key);
            if (entry?.Waypoint is Waypoint waypoint) datapad.ToggleWaypoint(waypoint);
            else if (entry?.SceneObject != null) interactions.ActivateDefault(entry.SceneObject.SwgId);
        }
        public void Select(string key)
        {
            selected = key;
            if (key != null) selections[CurrentTab] = key;
            for (int i = 0; i < cells.Count; i++) cells[i].selection.SetActive(entries[i].Key == key);
            var entry = entries.FirstOrDefault(item => item.Key == key);
            if (entry != null) schematics.RequestDetails(entry.SchematicCrc);
            RefreshDetails();
            RefreshPreview();
        }
        void RefreshPreview()
        {
            preview.Clear();
            var entry = entries.FirstOrDefault(item => item.Key == selected);
            if (entry == null) return;
            var request = Describe(entry);
            if (cache.TryGet(request.Key, out var result) && (result.Appearance.Parts.Count > 0 || result.Appearance.Particles.Count > 0))
                preview.Show(result.Appearance, result.Resources);
        }
        string Attribute(string name, string value) => "<color=#" + ColorUtility.ToHtmlStringRGB(theme.attributeText) + ">" + name + ":</color>    <noparse>" + value + "</noparse>\n";
        void RefreshDetails()
        {
            var entry = entries.FirstOrDefault(item => item.Key == selected);
            selectedName.text = entry?.Name ?? "";
            selectedKind.text = entry?.Kind ?? "";
            details.enabled = entry?.SceneObject != null;
            if (entry == null) { details.text.text = ""; return; }
            if (entry.SceneObject != null) { details.Show(entry.SceneObject.SwgId); return; }
            var text = new StringBuilder();
            if (entry.Waypoint.HasValue)
            {
                var waypoint = entry.Waypoint.Value;
                text.Append(Attribute("Planet", entry.Planet));
                text.Append(Attribute("Position", Mathf.RoundToInt(waypoint.Position.x) + ", " + Mathf.RoundToInt(waypoint.Position.y) + ", " + Mathf.RoundToInt(waypoint.Position.z)));
                text.Append(Attribute("Active", waypoint.Active ? "Yes" : "No"));
            }
            if (entry.SchematicCrc != 0 && schematics.TryGetDetails(entry.SchematicCrc, out var schematic))
            {
                text.Append(Attribute("Complexity", schematic.Complexity.ToString()));
                text.Append(Attribute("Schematic Data Size", schematic.Size.ToString()));
                foreach (var ingredient in schematic.Ingredients)
                    text.Append(Attribute(localizer.Localize(ingredient.Name), "Requires " + ingredient.Quantity + " units of " + ingredient.Resource + (ingredient.Optional ? " (optional)" : "")));
            }
            foreach (var group in schematics.GetExperiments(entry.SchematicCrc))
            {
                text.AppendLine().AppendLine(schematics.CraftingLabel(group.Name));
                foreach (var experiment in group.Properties)
                {
                    text.Append("    ").AppendLine(schematics.CraftingLabel(experiment.Name));
                    foreach (var weight in experiment.Weights)
                        text.Append("        ").Append(Attribute(weight.Attribute, weight.Percentage + "%"));
                }
            }
            if (!string.IsNullOrEmpty(entry.Description)) text.AppendLine().Append("<noparse>").Append(entry.Description).Append("</noparse>");
            details.text.text = text.ToString();
        }
        public void ToggleOpen()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf) transform.SetAsLastSibling();
        }
        public void Close() => gameObject.SetActive(false);
    }
}
