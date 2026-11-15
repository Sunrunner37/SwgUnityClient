using System;
using System.Collections;
using System.Linq;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Login;
using UnityEngine;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Assets.UI;
using UnityEngine.EventSystems;
namespace Swg.Client.Runtime.UI.Commands
{
    /// <summary>Prefab-authored slots with a draggable row count and a per-character local binding profile.</summary>
    public sealed class ActionBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Serializable] sealed class Profile { public int rows = 1; public string[] bindings; }
        public CommandButton[] slots;
        public GameObject[] rows;
        public RectTransform resizeHandle;
        public RectTransform shortcutLabels;
        public float rowHeight = 40;
        [Range(1, 5)] public int visibleRows = 1;
        public bool Ready { get; private set; }
        CommandCatalog catalog;
        CommandVisuals visuals;
        string profileKey;
        float startY;
        int startRows;
        bool resizing;
        void Awake()
        {
            catalog = GameServices.Resolve<CommandCatalog>();
            visuals = GameServices.Resolve<CommandVisuals>();
            var admission = GameServices.Resolve<Admission>();
            var player = GameServices.Resolve<CurrentPlayer>();
            var settings = GameServices.Resolve<ClientSettings>();
            profileKey = "ActionBar/" + settings.LoginEndpoint + "/" + admission.Username + "/" + admission.Galaxy.Name + "/" + player.SwgId;
            if (PlayerPrefs.HasKey(profileKey))
            {
                var profile = JsonUtility.FromJson<Profile>(PlayerPrefs.GetString(profileKey));
                visibleRows = profile.rows;
                for (int i = 0; i < Math.Min(slots.Length, profile.bindings.Length); i++) slots[i].SetBindingKey(profile.bindings[i]);
            }
            ApplyRows();
            foreach (var slot in slots) slot.BindingChanged += Save;
        }
        void OnEnable() { catalog.Changed += Refresh; StartCoroutine(Load()); }
        IEnumerator Load()
        {
            yield return new WaitUntil(() => visuals.Loading.IsCompleted);
            if (visuals.Loading.IsFaulted) { Debug.LogException(visuals.Loading.Exception.GetBaseException(), this); yield break; }
            Ready = true; Refresh();
        }
        void OnDisable() { StopAllCoroutines(); catalog.Changed -= Refresh; }
        void OnDestroy() { foreach (var slot in slots) if (slot != null) slot.BindingChanged -= Save; }
        void Refresh() { if (Ready) foreach (var slot in slots) if (slot.gameObject.activeInHierarchy) slot.RefreshBinding(); }
        void Save()
        {
            PlayerPrefs.SetString(profileKey, JsonUtility.ToJson(new Profile { rows = visibleRows, bindings = slots.Select(slot => slot.BindingKey).ToArray() }));
            PlayerPrefs.Save();
        }
        void ApplyRows()
        {
            visibleRows = Mathf.Clamp(visibleRows, 1, Mathf.Min(5, rows.Length == 0 ? 1 : rows.Length));
            for (int i = 0; i < rows.Length; i++) rows[i].SetActive(i < visibleRows);
            if (resizeHandle != null) resizeHandle.anchoredPosition = new Vector2(resizeHandle.anchoredPosition.x, -visibleRows * rowHeight - 25);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, visibleRows * rowHeight + 32);
            if (shortcutLabels != null) shortcutLabels.anchoredPosition = new Vector2(0, -visibleRows * rowHeight - 4);
            Refresh();
        }
        public void OnBeginDrag(PointerEventData pointer)
        {
            resizing = resizeHandle != null && RectTransformUtility.RectangleContainsScreenPoint(resizeHandle, pointer.pressPosition, pointer.pressEventCamera);
            if (!resizing) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, pointer.position, pointer.pressEventCamera, out var point);
            startY = point.y; startRows = visibleRows;
        }
        public void OnDrag(PointerEventData pointer)
        {
            if (!resizing) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, pointer.position, pointer.pressEventCamera, out var point);
            visibleRows = startRows + Mathf.RoundToInt((startY - point.y) / rowHeight);
            ApplyRows();
        }
        public void OnEndDrag(PointerEventData pointer) { if (resizing) Save(); resizing = false; }
    }
}
