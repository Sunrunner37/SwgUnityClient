using System.Collections.Generic;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Travel
{
    /// <summary>Renders Core3's ticket choices; the server decides which tickets are valid and whether boarding is allowed.</summary>
    public sealed class TicketSelectionWindow : MonoBehaviour, IUiPrefabSource
    {
        public GameObject window;
        public RectTransform content;
        public Button rowPrefab;
        public Button accept;
        public IEnumerable<GameObject> UiPrefabs { get { yield return rowPrefab.gameObject; } }
        readonly List<Button> rows = new List<Button>();
        ClientTravel travel;
        StringTableLocalizer localizer;
        ClientTravel.TicketSelection displayed;
        int selected = -1;

        void Awake()
        {
            travel = GameServices.Resolve<ClientTravel>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
        }
        void OnEnable() { travel.Changed += Refresh; Refresh(); }
        void OnDisable() => travel.Changed -= Refresh;
        void Refresh()
        {
            window.SetActive(travel.Tickets != null);
            if (ReferenceEquals(displayed, travel.Tickets)) return;
            displayed = travel.Tickets;
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            selected = -1;
            accept.interactable = false;
            if (displayed == null) return;
            for (int i = 0; i < displayed.Destinations.Length; i++)
            {
                int index = i;
                var row = Instantiate(rowPrefab, content);
                row.GetComponentInChildren<TMP_Text>().text = localizer.Localize(displayed.Destinations[i]);
                row.onClick.AddListener(() => Select(index));
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
            transform.SetAsLastSibling();
        }
        void Select(int row)
        {
            selected = row;
            accept.interactable = true;
            rows[row].Select();
        }
        public void Accept() => travel.UseTicket(selected);
        public void Cancel() => travel.CancelTickets();
    }
}
