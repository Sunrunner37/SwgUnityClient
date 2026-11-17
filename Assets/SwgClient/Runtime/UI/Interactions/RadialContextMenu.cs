using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.UI.Interactions
{
    public sealed class RadialContextMenu : MonoBehaviour, IUiPrefabSource
    {
        public GameObject overlay;
        public RectTransform menu;
        public Transform options;
        public RadialMenuOption optionPrefab;
        public IEnumerable<GameObject> UiPrefabs
        {
            get
            {
                yield return optionPrefab.gameObject;
            }
        }
        public GameObject back;
        public ExamineWindow examineWindow;
        public float radius = 70;
        public bool IsOpen => overlay.activeSelf;
        public RadialMenuOption Hovered { get; set; }
        ClientObjectInteractions interactions;
        StringTableLocalizer localizer;
        string[] captions = System.Array.Empty<string>();
        readonly List<RadialMenuOption> rows = new List<RadialMenuOption>();
        IReadOnlyList<RadialMenuEntry> localEntries;
        System.Action<RadialMenuEntry> localSelection;
        IEnumerable<RadialMenuEntry> Entries => localEntries ?? interactions.Menu;
        byte parent;
        int renderedFrame;

        void Awake()
        {
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
            overlay.SetActive(false);
        }
        void OnEnable()
        {
            interactions.MenuChanged += Refresh;
            interactions.ExamineRequested += examineWindow.Open;
            StartCoroutine(LoadCaptions());
        }
        IEnumerator LoadCaptions()
        {
            var read = GameServices.Resolve<TreRuntimeAssets>().ReadAsync(loader => DataTableFile.Parse(loader.Read("datatables/player/radial_menu.iff")).Rows.Select(row => row.Get<string>("caption")).ToArray());
            yield return new WaitUntil(() => read.IsCompleted);
            if (read.IsFaulted)
                Debug.LogException(read.Exception.GetBaseException(), this);
            else
            {
                captions = read.Result;
                Refresh();
            }
        }
        public void Open(SwgId swgId, Vector2 screenPosition)
        {
            localEntries = null;
            localSelection = null;
            Position(screenPosition);
            interactions.RequestMenu(swgId);
        }
        public void OpenLocal(Vector2 screenPosition, IReadOnlyList<RadialMenuEntry> entries, System.Action<RadialMenuEntry> select)
        {
            localEntries = entries;
            localSelection = select;
            Position(screenPosition);
            Refresh();
        }
        void Position(Vector2 screenPosition)
        {
            parent = 0;
            overlay.SetActive(true);
            transform.SetAsLastSibling();
            var canvas = GetComponentInParent<Canvas>();
            var area = (RectTransform)menu.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var point);
            point.x = Mathf.Clamp(point.x, area.rect.xMin + 220, area.rect.xMax - 220);
            point.y = Mathf.Clamp(point.y, area.rect.yMin + 130, area.rect.yMax - 130);
            menu.anchoredPosition = point;
        }
        public void Close() => overlay.SetActive(false);
        public void Back()
        {
            parent = 0;
            Refresh();
        }
        public void Select(RadialMenuEntry entry)
        {
            if (!IsOpen || entry.ParentIndex != parent)
                return;
            if (Entries.Any(item => item.ParentIndex == entry.Index))
            {
                parent = entry.Index;
                Refresh();
                return;
            }
            Close();
            if (localSelection != null) { localSelection(entry); return; }
            if (entry.RadialId == ObjectMenuAction.Examine.Id)
                examineWindow.Open(interactions.MenuTargetSwgId);
            else
                interactions.SelectMenuAction(interactions.MenuTargetSwgId, entry.RadialId, entry.Callback);
        }
        string Caption(RadialMenuEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.Text))
                return localizer.Localize(entry.Text);
            string reference = ObjectMenuAction.TryGet(entry.RadialId, out var action) ? action.Caption : "";
            if (!string.IsNullOrEmpty(reference))
                return localizer.Localize(reference);
            return entry.RadialId < captions.Length ? localizer.Lookup("ui_radial", captions[entry.RadialId].ToLowerInvariant()) : "Option " + entry.RadialId;
        }
        void Refresh()
        {
            if (!IsOpen)
                return;
            Hovered = null;
            foreach (var row in rows)
                Destroy(row.gameObject);
            rows.Clear();
            var entries = Entries.Where(item => item.ParentIndex == parent).ToArray();
            float step = entries.Length > 8 ? 360f / entries.Length : 45;
            for (int i = 0; i < entries.Length; i++)
            {
                var row = Instantiate(optionPrefab, options);
                row.Bind(this, entries[i], (i + 1) + ") " + Caption(entries[i]));
                var rect = (RectTransform)row.transform;
                float angle = i * step * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle);
                rect.pivot = new Vector2(x > .1f ? 0 : x < -.1f ? 1 : .5f, .5f);
                rect.anchoredPosition = new Vector2(x, Mathf.Cos(angle)) * radius;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(66, row.label.preferredWidth + 18));
                rows.Add(row);
            }
            back.SetActive(parent != 0);
            renderedFrame = Time.frameCount;
        }
        void LateUpdate()
        {
            if (!IsOpen)
                return;
            if (UiInput.TryConsumeEscape())
                Close();
            else if (Time.frameCount > renderedFrame && Mouse.current?.leftButton.wasReleasedThisFrame == true && Hovered != null)
                Hovered.Select();
        }
        void OnDisable()
        {
            interactions.MenuChanged -= Refresh;
            interactions.ExamineRequested -= examineWindow.Open;
            Close();
        }
    }
}
