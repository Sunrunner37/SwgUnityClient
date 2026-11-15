using System;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.Assets.Tre;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Runtime.UI.Theming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Swg.Client.Runtime.UI.Crafting
{
    public sealed class CraftingWindow : MonoBehaviour, IUiPrefabSource
    {
        [Serializable]
        public sealed class Step
        {
            public GameObject root;
            public ExaminePreview preview;
            public ExamineAttributes examine;
            public TMP_Text details;
            public Button next;
            public TMP_Text name;
            public TMP_Text complexity;
        }
        public UiTheme theme;
        public Step drafts;
        public Step assembly;
        public Step summary;
        public Step finish;
        public Step experimentation;
        public Step customization;
        public RectTransform tabs;
        public RectTransform choices;
        public Button rowPrefab;
        public Button tabPrefab;
        public Button folderPrefab;
        public TreVisual tabNormal;
        public TreVisual tabSelected;
        public TreVisual folderExpanded;
        public TreVisual folderCollapsed;
        public RectTransform materials;
        public ContainerItem itemPrefab;
        public ItemPreviewAtlas atlas;
        public RectTransform slots;
        public CraftingSlot slotPrefab;
        public RectTransform experimentRows;
        public RectTransform summaryRows;
        public CraftingExperimentRow summaryPrefab;
        public CraftingExperimentRow experimentPrefab;
        public TMP_Text experimentPoints;
        public TMP_Text risk;
        public CraftingExperimentMeter pointStack;
        public CraftingExperimentMeter riskAnimation;
        public Button experimentButton;
        public Button manufacturingButton;
        public Toggle filter;
        public TMP_InputField itemName;
        public Toggle practice;
        public Slider manufacturingRuns;
        public TMP_Text runsLabel;
        public GameObject popup;
        public TMP_Text popupText;
        public IEnumerable<GameObject> UiPrefabs => new[] { tabPrefab.gameObject, folderPrefab.gameObject, rowPrefab.gameObject, itemPrefab.gameObject, slotPrefab.gameObject, experimentPrefab.gameObject, summaryPrefab.gameObject };
        ClientCrafting crafting;
        ClientInventory inventory;
        DraftSchematicCatalog schematics;
        ClientObjectInteractions interactions;
        WorldState state;
        SceneObjectNameResolver names;
        StringTableLocalizer strings;
        ItemPreviewCache cache;
        WorldContentLoader loader;
        TreRuntimeAssets assets;
        readonly List<ContainerItem> cells = new List<ContainerItem>();
        readonly List<CraftingSlot> slotViews = new List<CraftingSlot>();
        readonly List<CraftingExperimentRow> summaryViews = new List<CraftingExperimentRow>();
        readonly List<CraftingExperimentRow> experiments = new List<CraftingExperimentRow>();
        readonly Dictionary<int, int> allocations = new Dictionary<int, int>();
        readonly HashSet<string> collapsed = new HashSet<string>();
        ClientCrafting.Choice shownChoice;
        ClientCrafting.Page page = (ClientCrafting.Page)(-1);
        SwgId selectedMaterial;
        uint selectedTab;
        int pointCapacity;
        bool dirty;
        bool rebuildChoices;
        Coroutine loading;
        string previewKey;
        void Awake()
        {
            loader = GameServices.Resolve<WorldContentLoader>();
            assets = GameServices.Resolve<TreRuntimeAssets>();
            crafting = GameServices.Resolve<ClientCrafting>(); inventory = GameServices.Resolve<ClientInventory>();
            schematics = GameServices.Resolve<DraftSchematicCatalog>(); interactions = GameServices.Resolve<ClientObjectInteractions>();
            state = GameServices.Resolve<WorldState>(); names = GameServices.Resolve<SceneObjectNameResolver>();
            strings = GameServices.Resolve<StringTableLocalizer>(); cache = GameServices.Resolve<ItemPreviewCache>();
            filter.onValueChanged.AddListener(_ => MarkDirty());
        }
        void OnEnable() { crafting.Changed += MarkDirty; crafting.IngredientOperationStarted += OnIngredientOperation; theme.Changed += MarkDirty; MarkDirty(); }
        void OnDisable() { crafting.Changed -= MarkDirty; crafting.IngredientOperationStarted -= OnIngredientOperation; theme.Changed -= MarkDirty; StopAllCoroutines(); loading = null; }
        void MarkDirty() => dirty = true;
        void OnIngredientOperation(int slot) => slotViews[slot].BeginLoading();
        Step[] Steps => new[] { drafts, assembly, summary, finish, experimentation, customization };
        void LateUpdate()
        {
            if (!dirty || !crafting.Loading.IsCompleted) return;
            dirty = false;
            if (shownChoice != crafting.Selected)
            {
                shownChoice = crafting.Selected;
                previewKey = null;
                rebuildChoices = true;
                if (loading != null) StopCoroutine(loading);
                loading = null;
            }
            bool changed = page != crafting.CurrentPage;
            page = crafting.CurrentPage;
            for (int i = 0; i < Steps.Length; i++) Steps[i].root.SetActive((int)page == i + 1);
            if (changed)
            {
                if (page != ClientCrafting.Page.Closed) transform.SetAsLastSibling();
                if (loading != null) StopCoroutine(loading);
                loading = null; previewKey = null;
                foreach (var step in Steps) step.preview?.Clear();
                if (page == ClientCrafting.Page.Drafts)
                {
                    pointCapacity = 0;
                    selectedTab = crafting.Choices.FirstOrDefault()?.Tab ?? 0;
                    BuildTabs(); rebuildChoices = true;
                }
                if (page == ClientCrafting.Page.Ingredients) BuildSlots();
                if (page == ClientCrafting.Page.Experiment) { allocations.Clear(); BuildExperiments(); }
                if (page == ClientCrafting.Page.Customize) itemName.text = crafting.Selected?.Entry.Name ?? "";
            }
            popup.SetActive(!string.IsNullOrEmpty(crafting.Popup)); popupText.text = crafting.Popup ?? "";
            if (page == ClientCrafting.Page.Closed)
            {
                Clear(materials); cells.Clear(); atlas.Clear();
                return;
            }
            if (page == ClientCrafting.Page.Drafts)
            {
                if (rebuildChoices) BuildChoices();
                drafts.details.text = DraftDetails();
                drafts.next.interactable = crafting.Selected != null && !crafting.Busy;
            }
            if (page == ClientCrafting.Page.Ingredients)
            {
                UpdateMaterials(); UpdateSlots(); assembly.details.text = DraftDetails();
                assembly.next.interactable = crafting.CanAssemble && !crafting.Busy;
            }
            if (page == ClientCrafting.Page.Summary)
            {
                summary.details.text = "The item assembly was a " + crafting.Result.ToLowerInvariant();
                UpdateSummary();
            }
            if (page == ClientCrafting.Page.Finish)
            {
                experimentButton.interactable = crafting.Progress.ServerStage == 3 && crafting.Progress.ExperimentationPoints > 0 && (crafting.Schematic?.ExperimentNames.Count ?? 0) > 0;
                manufacturingButton.interactable = crafting.AllowManufacturing;
            }
            if (page == ClientCrafting.Page.Experiment) UpdateExperiments();
            if (page == ClientCrafting.Page.Customize)
            {
                manufacturingRuns.gameObject.SetActive(crafting.Manufacturing);
                runsLabel.gameObject.SetActive(crafting.Manufacturing);
                practice.gameObject.SetActive(!crafting.Manufacturing);
                customization.next.interactable = !crafting.Busy;
            }
            if (changed && page >= ClientCrafting.Page.Summary)
            {
                foreach (var step in Steps.Where(step => step.root.activeSelf)) step.examine?.Show(crafting.PrototypeSwgId);
            }
            var current = Steps[(int)page - 1];
            if (current.name != null)
            {
                if (page == ClientCrafting.Page.Ingredients)
                    current.name.text = state.SceneObjects.TryGet(selectedMaterial, out var selectedItem) ? names.Resolve(selectedItem) : "";
                else current.name.text = (crafting.Selected?.Entry.Name ?? "") + (page == ClientCrafting.Page.Experiment ? "\n" + crafting.Selected?.Category : "");
            }
            if (current.complexity != null)
                current.complexity.text = page == ClientCrafting.Page.Ingredients || page == ClientCrafting.Page.Experiment ? "Complexity       " + (crafting.Schematic?.Complexity ?? 0) : "";
            if (current.preview != null && loading == null) loading = StartCoroutine(LoadPreview(current));
        }
        string Local(string file, string key) => strings.Lookup(file, key);
        string DraftDetails()
        {
            if (crafting.Selected == null) return "";
            var entry = crafting.Selected.Entry;
            if (!schematics.TryGetDetails(entry.SchematicCrc, out var details)) return entry.Description;
            var result = new StringBuilder((page == ClientCrafting.Page.Drafts ? "" : entry.Name + "\n" + entry.Description + "\n") + "<color=#80FF00>Complexity:</color> " + details.Complexity + "\n<color=#80FF00>Schematic Data Size:</color> " + details.Size + "\n");
            foreach (var ingredient in details.Ingredients)
                result.Append("<color=#FFDD00>").Append(strings.Localize(ingredient.Name)).AppendLine("</color>")
                    .Append("    Requires ").Append(ingredient.Quantity).Append(" units of ").Append(crafting.ResourceName(ingredient.Resource)).AppendLine(".");
            foreach (var group in schematics.GetExperiments(entry.SchematicCrc))
            {
                result.AppendLine().AppendLine(schematics.CraftingLabel(group.Name));
                foreach (var property in group.Properties)
                {
                    result.Append("  <color=#BEFF00>").Append(schematics.CraftingLabel(property.Name)).AppendLine("</color>");
                    foreach (var weight in property.Weights)
                    {
                        result.Append("      ").Append(weight.Attribute).Append(": ").Append(weight.Percentage).AppendLine("%");
                        if (page == ClientCrafting.Page.Ingredients && state.SceneObjects.TryGet<TangibleSceneObject>(selectedMaterial, out var input))
                        {
                            int value = crafting.ResourceValue(input, weight.Attribute);
                            result.Append("        <color=#BEFF00>").Append(names.Resolve(input)).Append(": ").Append(value).Append(" x ").Append(weight.Percentage).Append("% = ").Append(value * weight.Percentage / 100f).AppendLine("</color>");
                        }
                    }
                }
            }
            if (page == ClientCrafting.Page.Ingredients && state.SceneObjects.TryGet<TangibleSceneObject>(selectedMaterial, out var material))
                result.AppendLine().Append("<color=#80FF00>").Append(names.Resolve(material)).Append(": ").Append(Mathf.RoundToInt(crafting.Quality(material) * 1000)).AppendLine(" / 1000</color>");
            return result.ToString();
        }
        static string TabName(uint tab) => tab switch
                {
                    1 => "Weapon", 2 => "Armor", 4 => "Food", 8 => "Clothing", 16 => "Vehicle", 32 => "Droid", 64 => "Chemical", 128 => "Structure", 256 => "Furniture", 512 => "Misc", 524288 => "Misc", _ => "Items " + tab
                };
        void BuildTabs()
        {
            Clear(tabs);
            foreach (uint tab in crafting.Choices.Select(choice => choice.Tab).Distinct().OrderBy(TabName))
            {
                var button = Instantiate(tabPrefab, tabs);
                button.GetComponent<Image>().color = tab == selectedTab ? theme.accent : theme.tab;
                var outline = button.GetComponent<Outline>();
                if (outline != null) outline.effectColor = theme.accent;
                var binding = button.GetComponent<TreTextureBinding>();
                binding.asset = tab == selectedTab ? tabSelected : tabNormal;
                binding.Apply(assets.LoadVisual);
                var caption = button.GetComponentInChildren<TMP_Text>();
                caption.color = tab == selectedTab ? theme.onAccent : theme.bodyText;
                caption.text = TabName(tab);
                button.GetComponent<LayoutElement>().preferredWidth = Mathf.Ceil(caption.GetPreferredValues(caption.text).x);
                button.onClick.AddListener(() => { selectedTab = tab; BuildTabs(); rebuildChoices = true; MarkDirty(); });
            }
        }
        void BuildChoices()
        {
            rebuildChoices = false; Clear(choices);
            foreach (var group in crafting.Choices.Where(choice => choice.Tab == selectedTab).GroupBy(choice => choice.Category))
            {
                var folder = Instantiate(folderPrefab, choices);
                ((RectTransform)folder.transform).sizeDelta = new Vector2(0, 24);
                folder.GetComponentInChildren<TMP_Text>().text = group.Key;
                var folderArt = folder.GetComponentInChildren<TreTextureBinding>();
                folderArt.asset = collapsed.Contains(group.Key) ? folderCollapsed : folderExpanded;
                folderArt.Apply(assets.LoadVisual);
                folder.onClick.AddListener(() => { if (!collapsed.Add(group.Key)) collapsed.Remove(group.Key); rebuildChoices = true; MarkDirty(); });
                if (collapsed.Contains(group.Key)) continue;
                foreach (var choice in group.OrderBy(value => value.Entry.Name))
                {
                    var button = Instantiate(rowPrefab, choices);
                    ((RectTransform)button.transform).sizeDelta = new Vector2(0, 20);
                    button.GetComponentInChildren<TMP_Text>().text = "     " + choice.Entry.Name;
                    var colors = button.colors;
                    colors.normalColor = choice == crafting.Selected ? theme.accent : Color.clear;
                    button.colors = colors;
                    button.GetComponentInChildren<TMP_Text>().color = choice == crafting.Selected ? theme.onAccent : theme.bodyText;
                    button.onClick.AddListener(() => { crafting.Select(choice); previewKey = null; rebuildChoices = true; });
                }
            }
        }
        readonly Dictionary<string, Texture> resourceIcons = new Dictionary<string, Texture>();
        Texture ResourceIcon(string resource)
        {
            resource = resource == "chemical" ? "petrochem_inert" : resource;
            if (!resourceIcons.TryGetValue(resource, out var texture))
            {
                string exact = "texture/ui_res_" + resource + ".dds";
                string path = assets.Loader.Catalog().ContainsKey(exact) ? exact : assets.Loader.Catalog().Keys
                    .Where(key => key.StartsWith("texture/ui_res_") && key.EndsWith(".dds"))
                    .OrderByDescending(key => key.Length)
                    .FirstOrDefault(key => resource.StartsWith(key.Substring(15, key.Length - 19) + "_", StringComparison.OrdinalIgnoreCase));
                resourceIcons.Add(resource, texture = assets.LoadTexture(path ?? "texture/ui_res_mineral.dds"));
            }
            return texture;
        }
        void BuildSlots()
        {
            Clear(slots); slotViews.Clear();
            for (int i = 0; i < crafting.Slots.Length; i++)
            {
                int index = i;
                var slot = Instantiate(slotPrefab, slots);
                slot.Insert = () => crafting.Insert(selectedMaterial, index);
                slot.Remove = () => crafting.Remove(index);
                slotViews.Add(slot);
            }
        }
        void UpdateSlots()
        {
            state.SceneObjects.TryGet<TangibleSceneObject>(selectedMaterial, out var selected);
            for (int i = 0; i < slotViews.Count; i++)
            {
                var slot = slotViews[i]; var requirement = crafting.Slots[i];
                slot.label.text = strings.Localize("@" + requirement.Name.File + ":" + requirement.Name.Key);
                int quantity = crafting.Quantity(i);
                var material = crafting.SlotMaterial(i);
                bool qualifies = selected != null && quantity < requirement.Quantity && crafting.Qualifies(selected, requirement);
                float quality = quantity > 0 ? crafting.SlotQuality(i) : qualifies ? crafting.Quality(selected) : 0;
                slot.Show(ResourceIcon(material?.ResourceType ?? requirement.ResourceType), quantity, (int)requirement.Quantity, qualifies,
                    quality, crafting.Busy, theme.surface);
            }
        }
        static string MaterialQuantity(int quantity) => quantity >= 1000 ? (quantity / 1000) + "k" : quantity > 1 ? quantity.ToString() : "";
        void UpdateMaterials()
        {
            var items = (filter.isOn ? crafting.Materials : inventory.Items).OrderBy(item => names.Resolve(item)).ToArray();
            bool changed = !cells.Select(cell => cell.SwgId).SequenceEqual(items.Select(item => item.SwgId));
            if (!changed)
            {
                for (int i = 0; i < cells.Count; i++) cells[i].quantity.text = MaterialQuantity(items[i].Quantity);
                return;
            }
            Clear(materials); cells.Clear(); atlas.Begin(items.Length);
            foreach (var item in items)
            {
                var cell = Instantiate(itemPrefab, materials);
                cell.Bind(item.SwgId, () => SelectMaterial(item.SwgId), () =>
                {
                    SelectMaterial(item.SwgId);
                    int index = Array.FindIndex(crafting.Slots, slot => crafting.Qualifies(item, slot) && crafting.Quantity(Array.IndexOf(crafting.Slots, slot)) < slot.Quantity);
                    crafting.Insert(item.SwgId, index);
                });
                cell.itemName.text = names.Resolve(item); cell.quantity.text = MaterialQuantity(item.Quantity);
                cell.equipped.SetActive(false); cell.selection.SetActive(item.SwgId == selectedMaterial);
                var drag = cell.gameObject.AddComponent<CraftingIngredientDrag>(); drag.icon = cell.image; drag.Select = () => SelectMaterial(item.SwgId);
                cells.Add(cell);
            }
            StartCoroutine(PrepareMaterials(items));
        }
        IEnumerator PrepareMaterials(TangibleSceneObject[] items)
        {
            var requests = items.Select(loader.DescribeAppearance).ToArray();
            yield return cache.Prepare(requests);
            if (page != ClientCrafting.Page.Ingredients || !items.Select(item => item.SwgId).SequenceEqual(cells.Select(cell => cell.SwgId))) yield break;
            for (int i = 0; i < requests.Length; i++)
                if (cache.TryGet(requests[i].Key, out var result)) atlas.Show(i, result.Appearance, result.Resources, cells[i]);
            atlas.RenderNow();
        }
        void SelectMaterial(SwgId id)
        {
            selectedMaterial = id;
            foreach (var cell in cells) cell.selection.SetActive(cell.SwgId == id);
            assembly.examine.Show(id); MarkDirty();
        }
        IEnumerator LoadPreview(Step step)
        {
            yield return null;
            AppearanceRequest request;
            if (page == ClientCrafting.Page.Drafts)
            {
                if (crafting.Selected == null) { loading = null; yield break; }
                request = new AppearanceRequest(crafting.Selected.Entry.PreviewPath);
            }
            else
            {
                if (!state.SceneObjects.TryGet(crafting.PrototypeSwgId, out var item)) { loading = null; yield break; }
                request = loader.DescribeAppearance(item);
            }
            if (previewKey == request.Key) { loading = null; yield break; }
            yield return cache.Prepare(new[] { request });
            if (cache.TryGet(request.Key, out var result)) { step.preview.Show(result.Appearance, result.Resources); previewKey = request.Key; }
            loading = null;
        }
        void UpdateSummary()
        {
            var schematic = crafting.Schematic;
            if (schematic == null) return;
            if (summaryViews.Count != schematic.ExperimentNames.Count)
            {
                Clear(summaryRows); summaryViews.Clear();
                foreach (string name in schematic.ExperimentNames) summaryViews.Add(Instantiate(summaryPrefab, summaryRows));
            }
            for (int i = 0; i < summaryViews.Count; i++) summaryViews[i].Show(schematics.CraftingLabel(schematic.ExperimentNames[i]), schematic.ExperimentValues.ElementAtOrDefault(i), 1, 0, i % 2 == 0);
        }
        void BuildExperiments()
        {
            Clear(experimentRows); experiments.Clear();
            for (int i = 0; i < (crafting.Schematic?.ExperimentNames.Count ?? 0); i++)
            {
                int row = i;
                var view = Instantiate(experimentPrefab, experimentRows);
                view.Bind(points =>
                {
                    int allocated = allocations.Where(pair => pair.Key != row).Sum(pair => pair.Value);
                    allocations[row] = Mathf.Min(allocations.GetValueOrDefault(row) == points ? 0 : points, crafting.Progress.ExperimentationPoints - allocated);
                    MarkDirty();
                });
                experiments.Add(view);
            }
        }
        void UpdateExperiments()
        {
            var schematic = crafting.Schematic;
            if (schematic == null) return;
            if (experiments.Count != schematic.ExperimentNames.Count) BuildExperiments();
            experimentPoints.text = (crafting.Progress.ExperimentationPoints - allocations.Values.Sum()).ToString();
            pointCapacity = Mathf.Max(pointCapacity, crafting.Progress.ExperimentationPoints);
            pointStack.ShowPoints(crafting.Progress.ExperimentationPoints - allocations.Values.Sum(), pointCapacity);
            risk.text = Mathf.RoundToInt(schematic.ExperimentRisk) + "%";
            riskAnimation.ShowRisk(schematic.ExperimentRisk);
            for (int i = 0; i < experiments.Count; i++) experiments[i].Show(schematics.CraftingLabel(schematic.ExperimentNames[i]), schematic.ExperimentValues.ElementAtOrDefault(i), schematic.ExperimentMaximums.ElementAtOrDefault(i), allocations.GetValueOrDefault(i));
            experimentation.next.interactable = !crafting.Busy && allocations.Values.Sum() > 0;
        }
        public void NextDraft() => crafting.BeginAssembly();
        public void Assemble() => crafting.Assemble();
        public void NextSummary() => crafting.ShowFinish();
        public void Experiment() => crafting.ShowExperiment();
        public void DoneExperimenting() => crafting.DoneExperimenting();
        public void Prototype() => crafting.ChooseOutput(false);
        public void Manufacturing() => crafting.ChooseOutput(true);
        public void RunExperiment()
        {
            crafting.Experiment(allocations.Where(pair => pair.Value > 0).Select(pair => (pair.Key, pair.Value)).ToArray());
            allocations.Clear();
        }
        public void Create() => crafting.Create(itemName.text, practice.isOn, Mathf.RoundToInt(manufacturingRuns.value));
        public void RunsChanged(float value) => runsLabel.text = "Manufacturing runs: " + Mathf.RoundToInt(value);
        public void ClosePopup() => crafting.DismissPopup();
        public void Back() => crafting.Back();
        public void Cancel() => crafting.Cancel();
        static void Clear(Transform parent) { foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
    }
}
