using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.UI.Containers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Swg.Client.Runtime.UI.Travel
{
    public sealed class TravelWindow : MonoBehaviour, IUiPrefabSource
    {
        [Serializable]
        public sealed class PlanetView
        {
            public string id;
            public string appearance;
            public GameObject map;
            public Button button;
            public RawImage preview;
            public GameObject glow;
            public TMP_Text label;
        }
        [Serializable]
        public sealed class RouteView
        {
            public RectTransform line;
            public RectTransform from;
            public RectTransform to;
        }
        public RouteView[] routes;
        public GameObject panel;
        public GameObject confirmation;
        public TMP_Text confirmationPrompt;
        public GameObject planetaryMap;
        public GameObject galacticMap;
        public RectTransform markers;
        public Button markerPrefab;
        public PlanetView[] planets;
        public TMP_Dropdown planetSelector;
        public TMP_Dropdown locationSelector;
        public TMP_Text mapToggleLabel;
        public TMP_Text price;
        public TMP_Text loading;
        public Toggle showNames;
        public Toggle roundTrip;
        public Button purchase;
        public ItemPreviewAtlas previews;
        public IEnumerable<GameObject> UiPrefabs
        {
            get
            {
                yield return markerPrefab.gameObject;
            }
        }
        readonly List<Button> destinations = new List<Button>();
        ClientTravel travel;
        ItemPreviewCache cache;
        bool galaxy;
        bool dirty;
        Vector2 routeLayoutSize;
        Coroutine preparing;
        bool appearancesReady;
        string markerPlanet;
        string[] planetIds = Array.Empty<string>();
        string[] pointNames = Array.Empty<string>();
        void Awake()
        {
            travel = GameServices.Resolve<ClientTravel>();
            cache = GameServices.Resolve<ItemPreviewCache>();
            confirmationPrompt.text = GameServices.Resolve<StringTableLocalizer>().Localize("@travel:ticket_purchase_complete");
            planetSelector.onValueChanged.AddListener(SelectPlanet);
            locationSelector.onValueChanged.AddListener(SelectLocation);
            roundTrip.onValueChanged.AddListener(travel.SetRoundTrip);
            showNames.onValueChanged.AddListener(_ => Changed());
            foreach (var planet in planets)
                planet.button.onClick.AddListener(() => { travel.SelectPlanet(planet.id); galaxy = false; Changed(); });
        }
        void OnEnable()
        {
            travel.Changed += Changed;
            Changed();
        }
        void OnDisable()
        {
            travel.Changed -= Changed;
            StopPreparing();
        }
        void Changed() => dirty = true;
        void StopPreparing()
        {
            if (preparing != null)
                StopCoroutine(preparing);
            preparing = null;

        }
        void LateUpdate()
        {
            if (galaxy && galacticMap.activeInHierarchy)
            {
                var size = ((RectTransform)galacticMap.transform).rect.size;
                if (size != routeLayoutSize)
                {
                    routeLayoutSize = size;
                    dirty = true;
                }
            }
            if (!dirty)
                return;
            dirty = false;
            confirmation.SetActive(travel.PurchaseComplete);
            if (travel.PurchaseComplete)
                transform.SetAsLastSibling();
            if (!travel.IsOpen)
            {
                if (panel.activeSelf)
                    StopPreparing();
                panel.SetActive(false);
                return;
            }
            if (!panel.activeSelf)
            {
                galaxy = false;
                panel.SetActive(true);
                transform.SetAsLastSibling();
            }
            planetaryMap.SetActive(!galaxy);
            galacticMap.SetActive(galaxy);
            mapToggleLabel.text = galaxy ? "Show Planetary Map" : "Show Galactic Map";
            RefreshDestinations();
            RefreshSelection();
            roundTrip.SetIsOnWithoutNotify(travel.RoundTrip);
            price.text = travel.Selected == null ? "-" : travel.Cost.ToString("N0");
            purchase.interactable = travel.CanPurchase;
            loading.gameObject.SetActive(pointNames.Length == 0);
            loading.text = "Loading destinations...";
            foreach (var planet in planets)
            {
                bool reachable = planetIds.Contains(planet.id);
                planet.map.SetActive(planet.id == travel.SelectedPlanet);
                planet.button.interactable = reachable;
                planet.preview.color = reachable ? Color.white : new Color(.3f, .3f, .3f, 1);
                planet.glow.SetActive(reachable);
                planet.label.gameObject.SetActive(showNames.isOn && reachable);
            }
            if (galaxy)
                foreach (var route in routes)
                {
                    var area = (RectTransform)galacticMap.transform;
                    Vector2 Center(RectTransform rect) => Vector2.Scale((rect.anchorMin + rect.anchorMax) * .5f - Vector2.one * .5f, area.rect.size);
                    var from = Center(route.from);
                    var to = Center(route.to);
                    var direction = (to - from).normalized;
                    float begin = route.from.rect.width * .55f;
                    float end = route.to.rect.width * .55f;
                    route.line.anchoredPosition = (from + to) * .5f + direction * (begin - end) * .5f;
                    route.line.sizeDelta = new Vector2(Mathf.Max(0, Vector2.Distance(from, to) - begin - end), 1);
                    route.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
            if (galaxy && previews.previewCamera.targetTexture == null)
            {
                if (appearancesReady)
                    BuildPlanetPreviews();
                else if (preparing == null)
                    preparing = StartCoroutine(PreparePlanets());
            }
        }
        void RefreshDestinations()
        {
            var nextPlanets = travel.Planets.ToArray();
            if (!planetIds.SequenceEqual(nextPlanets))
            {
                planetIds = nextPlanets;
                planetSelector.ClearOptions();
                planetSelector.AddOptions(planetIds.ToList());
            }
            var points = travel.Destinations.ToArray();
            var nextNames = points.Select(point => point.Name).ToArray();
            bool membershipChanged = !pointNames.SequenceEqual(nextNames);
            if (membershipChanged)
            {
                pointNames = nextNames;
                locationSelector.ClearOptions();
                locationSelector.AddOptions(pointNames.ToList());
            }
            if (membershipChanged || markerPlanet != travel.SelectedPlanet)
            {
                markerPlanet = travel.SelectedPlanet;
                foreach (var marker in destinations)
                    Destroy(marker.gameObject);
                destinations.Clear();
                foreach (var point in points)
                {
                    var marker = Instantiate(markerPrefab, markers);
                    marker.GetComponentInChildren<TMP_Text>(true).text = point.Name;
                    marker.onClick.AddListener(() => travel.SelectPoint(point.Name));
                    marker.gameObject.SetActive(true);
                    destinations.Add(marker);
                }
            }
            for (int i = 0; i < points.Length; i++)
            {
                var rect = (RectTransform)destinations[i].transform;
                rect.anchorMin = rect.anchorMax = MapPosition(points[i].SwgPosition);
                rect.anchoredPosition = Vector2.zero;
            }
        }

        void RefreshSelection()
        {
            planetSelector.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(planetIds, travel.SelectedPlanet)));
            locationSelector.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(pointNames, travel.SelectedPoint)));
            for (int i = 0; i < destinations.Count; i++)
                destinations[i].GetComponentInChildren<TMP_Text>(true).color = pointNames[i] == travel.SelectedPoint
                    ? Color.white : new Color32(150, 255, 0, 255);
        }

        // SWG positions use X/Y for the ground plane and Z for elevation.
        internal static Vector2 MapPosition(Vector3 serverPosition) => new Vector2(
            Mathf.Clamp01(.5f + serverPosition.x / 16384),
            Mathf.Clamp01(.5f + serverPosition.y / 16384));

        public IEnumerator Prewarm()
        {
            if (appearancesReady)
                yield break;
            // World entry calls this while the HUD is inactive, before Awake may run.
            var previewsCache = GameServices.Resolve<ItemPreviewCache>();
            yield return previewsCache.Prepare(planets.Select(planet => new AppearanceRequest(planet.appearance)));
            appearancesReady = true;
        }

        IEnumerator PreparePlanets()
        {
            yield return Prewarm();
            BuildPlanetPreviews();
            preparing = null;
        }

        void BuildPlanetPreviews()
        {
            previews.Begin(planets.Length);
            for (int i = 0; i < planets.Length; i++)
                if (cache.TryGet(new AppearanceRequest(planets[i].appearance).Key, out var preview))
                    previews.Show(i, preview.Appearance, preview.Resources, planets[i].preview, planets[i].id);
            previews.RenderNow();
        }
        void SelectPlanet(int index)
        {
            if (index < planetIds.Length)
            {
                travel.SelectPlanet(planetIds[index]);
                galaxy = false;
                Changed();
            }
        }
        void SelectLocation(int index)
        {
            if (index < pointNames.Length)
                travel.SelectPoint(pointNames[index]);
        }
        public void ToggleMap()
        {
            galaxy = !galaxy;
            Changed();
        }
        public void Purchase() => travel.Purchase();
        public void AcknowledgePurchase() => travel.AcknowledgePurchase();
        public void Close() => travel.Close();
    }
}
