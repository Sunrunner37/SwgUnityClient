using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Runtime.UI.Theming;
namespace Swg.Client.Runtime.UI.Missions
{
    /// <summary>Editable mission-browser, detail, download and confirmation windows.</summary>
    public sealed class MissionWindows : MonoBehaviour, IUiPrefabSource
    {
        public UiTheme theme;
        public GameObject browser;
        public GameObject details;
        public GameObject downloading;
        public GameObject abortConfirmation;
        public RectTransform tabs;
        public RectTransform rows;
        public Button tabPrefab;
        public TreVisual tabNormal;
        public TreVisual tabSelected;
        public MissionRow rowPrefab;
        public Button accept;
        public Button viewDetails;
        public Button detailAccept;
        public TMP_Text detailTitle;
        public TMP_Text attributes;
        public TMP_Text description;
        public ExaminePreview preview;
        public Image[] downloadBlocks;
        public IEnumerable<GameObject> UiPrefabs => new[] { rowPrefab.gameObject, tabPrefab.gameObject };
        ClientMissions missions;
        TreRuntimeAssets assets;
        ItemPreviewCache cache;
        readonly List<MissionRow> cells = new List<MissionRow>();
        string category;
        string categoriesKey;
        SwgId[] renderedIds = Array.Empty<SwgId>();
        string previewPath;
        Coroutine loadingPreview;
        bool dirty;
        bool wasLoading;
        float loaderUntil;
        float timeout;
        void Awake()
        {
            missions = GameServices.Resolve<ClientMissions>();
            assets = GameServices.Resolve<TreRuntimeAssets>();
            cache = GameServices.Resolve<ItemPreviewCache>();
        }
        void OnEnable() { missions.Changed += Changed; theme.Changed += Changed; Changed(); }
        void OnDisable()
        {
            missions.Changed -= Changed; theme.Changed -= Changed;
            StopAllCoroutines(); loadingPreview = null; previewPath = null; preview.Clear();
        }
        void Changed()
        {
            dirty = true;
            if (missions.Loading && !wasLoading) { loaderUntil = Time.unscaledTime + .2f; timeout = Time.unscaledTime + 15; }
            wasLoading = missions.Loading;
        }
        void Visible(GameObject window, bool shown)
        {
            if (shown && !window.activeSelf) { transform.SetAsLastSibling(); window.transform.SetAsLastSibling(); }
            window.SetActive(shown);
        }
        void LateUpdate()
        {
            if (missions.Loading && Time.unscaledTime >= timeout)
            {
                Debug.LogWarning("Mission list download timed out."); missions.Close();
            }
            bool showLoader = missions.BrowserOpen && (missions.Loading || Time.unscaledTime < loaderUntil);
            Visible(downloading, showLoader);
            if (showLoader)
                for (int i = 0; i < downloadBlocks.Length; i++)
                {
                    int phase = (i + downloadBlocks.Length - Mathf.FloorToInt(Time.unscaledTime * 9) % downloadBlocks.Length) % downloadBlocks.Length;
                    downloadBlocks[i].color = new Color(theme.accent.r, theme.accent.g, theme.accent.b, phase < 4 ? 1 - phase * .18f : .12f);
                }
            Visible(browser, missions.BrowserOpen && !showLoader);
            Visible(details, missions.DetailsOpen && missions.Selected != null);
            Visible(abortConfirmation, !missions.AbortCandidate.IsNone);
            if (!dirty) return;
            dirty = false;
            if (missions.BrowserOpen) RefreshRows();
            accept.interactable = viewDetails.interactable = missions.Selected != null && !missions.Loading;
            accept.interactable = missions.CanAccept;
            detailAccept.gameObject.SetActive(missions.Selected != null && !missions.IsAccepted(missions.Selected));
            detailAccept.interactable = missions.CanAccept;
            if (missions.DetailsOpen && missions.Selected != null) RefreshDetails(missions.Selected);
            else if (!missions.DetailsOpen) { preview.Clear(); previewPath = null; if (loadingPreview != null) StopCoroutine(loadingPreview); loadingPreview = null; }
        }
        void RefreshRows()
        {
            var offers = missions.Available.ToArray();
            var categories = offers.Select(missions.Category).Distinct().OrderBy(name => name == "Destroy" ? 0 : name == "Deliver" ? 1 : 2).ThenBy(name => name).ToArray();
            if (!categories.Contains(category)) category = categories.FirstOrDefault();
            string key = string.Join("|", categories) + ":" + category;
            if (categoriesKey != key)
            {
                categoriesKey = key;
                foreach (Transform child in tabs) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                foreach (string name in categories)
                {
                    var button = Instantiate(tabPrefab, tabs);
                    var label = button.GetComponentInChildren<TMP_Text>(); label.text = name;
                    label.color = category == name ? theme.onAccent : theme.bodyText;
                    button.GetComponent<Image>().color = category == name ? theme.accent : theme.tab;
                    var binding = button.GetComponent<TreTextureBinding>(); binding.asset = category == name ? tabSelected : tabNormal; binding.Apply(assets.LoadVisual);
                    button.GetComponent<LayoutElement>().preferredWidth = Mathf.Ceil(label.GetPreferredValues(name).x);
                    button.onClick.AddListener(() => { category = name; categoriesKey = null; dirty = true; });
                }
            }
            var shown = offers.Where(mission => missions.Category(mission) == category).ToArray();
            var ids = shown.Select(mission => mission.SwgId).ToArray();
            if (!renderedIds.SequenceEqual(ids))
            {
                foreach (var cell in cells) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
                cells.Clear(); renderedIds = ids;
                foreach (var mission in shown)
                {
                    var row = Instantiate(rowPrefab, rows); var id = mission.SwgId;
                    row.Bind(() => missions.Select(id), () => missions.ShowDetails(id)); cells.Add(row);
                }
            }
            for (int i = 0; i < shown.Length; i++)
            {
                var mission = shown[i]; var row = cells[i];
                row.creator.text = mission.Creator; row.title.text = missions.Title(mission);
                row.reward.text = mission.Reward.ToString(); row.location.text = missions.Location(mission);
                row.background.color = mission.SwgId == missions.SelectedSwgId ? new Color32(53, 203, 215, 255) : i % 2 == 0 ? new Color32(0, 110, 135, 170) : new Color32(0, 121, 145, 170);
                var textColor = mission.SwgId == missions.SelectedSwgId ? theme.onAccent : theme.bodyText;
                row.creator.color = row.title.color = row.reward.color = row.location.color = textColor;
            }
        }
        void RefreshDetails(MissionSceneObject mission)
        {
            detailTitle.text = missions.Title(mission).ToUpperInvariant();
            attributes.text = "<line-height=24>" + missions.DetailText(mission);
            description.text = missions.Description(mission);
            string path = missions.TargetPath(mission);
            if (previewPath == path) return;
            previewPath = path; preview.Clear();
            if (loadingPreview != null) StopCoroutine(loadingPreview);
            loadingPreview = path == null ? null : StartCoroutine(LoadPreview(path));
        }
        IEnumerator LoadPreview(string path)
        {
            var request = new AppearanceRequest(path);
            yield return cache.Prepare(new[] { request });
            if (missions.DetailsOpen && previewPath == path && cache.TryGet(request.Key, out var result)) preview.Show(result.Appearance, result.Resources);
            loadingPreview = null;
        }
        public void Refresh() => missions.Refresh();
        public void Accept() => missions.Accept();
        public void ViewDetails() { if (missions.Selected != null) missions.ShowDetails(missions.SelectedSwgId); }
        public void Close() => missions.Close();
        public void CloseDetails() => missions.CloseDetails();
        public void ConfirmAbort() => missions.ConfirmAbort();
        public void CancelAbort() => missions.CancelAbort();
    }
}

