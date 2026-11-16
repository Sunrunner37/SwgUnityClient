using Swg.Client.Runtime.UI.Containers;
using System.Collections;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Core.Assets.Appearances;
using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Inventory
{
    public sealed class InventoryWindow : MonoBehaviour, IUiPrefabSource
    {
        public TMP_Text title;
        public RadialContextMenu contextMenu;
        ClientObjectInteractions interactions;
        public TMP_Text capacity;
        public TMP_Text cash;
        public TMP_Text bank;
        public CapacityBar capacityBar;
        public RectTransform examinePanel;
        public RectTransform characterPanel;
        public RectTransform centerPanel;
        public ExamineAttributes details;
        public ExaminePreview characterPreview;
        public TMP_Text characterName;
        public GridLayoutGroup grid;
        public ContainerItem itemPrefab;
        public ItemPreviewAtlas previews;
        public Toggle examineToggle;
        public Toggle characterToggle;
        public IEnumerable<GameObject> UiPrefabs
        {
            get
            {
                yield return itemPrefab.gameObject;
            }
        }
        readonly List<ContainerItem> cells = new List<ContainerItem>();
        ClientInventory inventory;
        SceneObjectNameResolver names;
        InventoryPreviewCache cache;
        SwgId selected;
        SwgId containerSwgId;
        bool PlayerInventory => containerSwgId.IsNone;
        ItemPreviewCache sharedPreviews;
        WorldContentLoader loader;
        Coroutine preparing;
        public void OpenContainer(SwgId swgId)
        {
            containerSwgId = swgId;
            selected = default;
            gameObject.SetActive(true);
            dirty = true;
            transform.SetAsLastSibling();
        }
        bool dirty;
        readonly Dictionary<SwgId, string> appearanceKeys = new Dictionary<SwgId, string>();
        string playerAppearanceKey;
        WorldState state;
        void Awake()
        {
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            state = GameServices.Resolve<WorldState>();
            inventory = GameServices.Resolve<ClientInventory>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
            cache = GameServices.Resolve<InventoryPreviewCache>();
            sharedPreviews = GameServices.Resolve<ItemPreviewCache>();
            loader = GameServices.Resolve<WorldContentLoader>();
        }
        void OnEnable()
        {
            if (PlayerInventory) inventory.Changed += Changed;
            if (PlayerInventory) cache.Changed += Changed;
            state.SceneObjects.ObjectChanged += ObjectChanged;
            state.SceneObjects.Removed += ObjectRemoved;
            examineToggle.onValueChanged.AddListener(SetExamineVisible);
            characterToggle.onValueChanged.AddListener(SetCharacterVisible);
            dirty = true;
            SetExamineVisible(examineToggle.isOn);
            SetCharacterVisible(characterToggle.isOn);
        }
        void OnDisable()
        {
            inventory.Changed -= Changed;
            cache.Changed -= Changed;
            state.SceneObjects.ObjectChanged -= ObjectChanged;
            state.SceneObjects.Removed -= ObjectRemoved;
            if (preparing != null) StopCoroutine(preparing);
            preparing = null;
            examineToggle.onValueChanged.RemoveListener(SetExamineVisible);
            characterToggle.onValueChanged.RemoveListener(SetCharacterVisible);
            details.Clear();
            previews.Clear();
            characterPreview.Clear();
            foreach (var cell in cells)
                Destroy(cell.gameObject);
            cells.Clear();
            appearanceKeys.Clear();
            playerAppearanceKey = null;
        }
        void Changed() => dirty = true;
        void ObjectChanged(SceneObject item, SceneObjectChange changes)
        {
            if (!PlayerInventory && (changes & (SceneObjectChange.Inventory | SceneObjectChange.Appearance | SceneObjectChange.Containment | SceneObjectChange.Replaced)) != 0
                && (item.ParentSwgId == containerSwgId || item.SwgId == containerSwgId || cells.Any(cell => cell.SwgId == item.SwgId))) dirty = true;
        }
        void ObjectRemoved(SceneObject item)
        {
            if (item.SwgId == containerSwgId) Close();
            else if (!PlayerInventory && cells.Any(cell => cell.SwgId == item.SwgId)) dirty = true;
        }
        void LateUpdate()
        {
            if (!dirty)
                return;
            dirty = false;
            Refresh();
        }
        void Refresh()
        {
            int used = PlayerInventory ? inventory.UsedCapacity : inventory.Used(containerSwgId);
            int maximum = PlayerInventory ? inventory.Capacity : inventory.Limit(containerSwgId);
            capacity.text = "Capacity                          " + (maximum > 0 ? Mathf.RoundToInt(used * 100f / maximum) + "% (" + used + "/" + maximum + ")" : used.ToString());
            capacityBar.Fill = maximum > 0 ? used / (float)maximum : 0;
            cash.text = "Cash: " + (inventory.Player?.CashCredits ?? 0).ToString("N0");
            bank.text = "Bank: " + (inventory.Player?.BankCredits ?? 0).ToString("N0");
            var items = (PlayerInventory ? inventory.Items : inventory.Contents(containerSwgId)).OrderBy(item => names.Resolve(item), StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.SwgId.Value).ToArray();
            bool membershipChanged = cells.Count != items.Length || !cells.Select(cell => cell.SwgId).SequenceEqual(items.Select(item => item.SwgId));
            if (membershipChanged)
            {
                foreach (var cell in cells)
                    Destroy(cell.gameObject);
                cells.Clear();
                appearanceKeys.Clear();
                previews.Begin(items.Length);
                foreach (var item in items)
                {
                    var cell = Instantiate(itemPrefab, grid.transform);
                    cell.Bind(item.SwgId, () => Select(item.SwgId), () => inventory.Activate(item.SwgId), position => contextMenu.Open(item.SwgId, position));

                    cells.Add(cell);
                }
                if (!items.Any(item => item.SwgId == selected))
                    selected = items.Length > 0 ? items[0].SwgId : default;
                Select(selected);
            }
            if (items.Length == 0) Select(default);
            for (int i = 0; i < items.Length; i++)
                UpdateCell(cells[i], items[i]);
            RefreshPreviews();
        }
        void UpdateCell(ContainerItem cell, TangibleSceneObject item)
        {
            cell.CanActivate = true; // Examine is the fallback double-click action.
            ObjectTypeId type = interactions.ObjectType(item);
            cell.ActivationCursor = type.IsContainer() ? ClientCursor.WorldCursor.Use
                : type.IsConsumable() ? ClientCursor.WorldCursor.Eat
                : interactions.HasDefaultAction(item.SwgId) || ClientInventory.CanEquip(item) ? ClientCursor.WorldCursor.Default : ClientCursor.WorldCursor.Examine;
            cell.itemName.text = names.Resolve(item);
            cell.quantity.text = item.Quantity > 1 ? (item.Quantity >= 1000 ? (item.Quantity / 1000).ToString() + "k" : item.Quantity.ToString()) : "";
            cell.equipped.SetActive(inventory.IsEquipped(item));
            cell.selection.SetActive(cell.SwgId == selected);
        }
        void RefreshPreviews()
        {
            var missing = new List<AppearanceRequest>();
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                ItemPreviewCache.Preview preview;
                if (PlayerInventory)
                {
                    if (!cache.TryGet(cell.SwgId, out preview)) continue;
                }
                else
                {
                    if (!state.SceneObjects.TryGet(cell.SwgId, out var item)) continue;
                    var request = loader.DescribeAppearance(item);
                    if (!sharedPreviews.TryGet(request.Key, out preview)) { missing.Add(request); continue; }
                }
                string key = preview.Appearance.Key;
                if (appearanceKeys.TryGetValue(cell.SwgId, out var previous) && previous == key)
                    continue;
                previews.Show(i, preview.Appearance, preview.Resources, cell);
                appearanceKeys[cell.SwgId] = key;
            }
            if (missing.Count > 0 && preparing == null) preparing = StartCoroutine(Prepare(missing));
            var player = PlayerInventory ? inventory.Player : null;
            if (player != null)
            {
                characterName.text = names.Resolve(player);
                if (cache.TryGet(player.SwgId, out var preview) && playerAppearanceKey != preview.Appearance.Key)
                {
                    characterPreview.Show(preview.Appearance, preview.Resources);
                    playerAppearanceKey = preview.Appearance.Key;
                }
            }
            previews.RenderNow();
        }
        IEnumerator Prepare(List<AppearanceRequest> requests)
        {
            yield return null;
            yield return sharedPreviews.Prepare(requests);
            preparing = null;
            dirty = true;
        }
        public void Select(SwgId swgId)
        {
            selected = swgId;
            foreach (var cell in cells)
                cell.selection.SetActive(cell.SwgId == selected);
            if (state.SceneObjects.TryGet(swgId, out var item))
            {
                title.text = names.Resolve(item).ToUpperInvariant();
                details.Show(swgId);
            }
            else
            {
                details.Clear();
                if (!PlayerInventory && state.SceneObjects.TryGet(containerSwgId, out var container)) title.text = names.Resolve(container).ToUpperInvariant();
            }
        }
        public void ToggleOpen()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf)
                transform.SetAsLastSibling();
        }
        public void Close() => gameObject.SetActive(false);
        public void SetExamineVisible(bool visible) => examinePanel.gameObject.SetActive(visible);
        public void SetCharacterVisible(bool visible) => characterPanel.gameObject.SetActive(visible);
    }
}
