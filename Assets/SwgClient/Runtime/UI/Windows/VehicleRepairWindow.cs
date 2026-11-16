using System;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;

namespace Swg.Client.Runtime.UI.Windows
{
    /// <summary>Displays a server-provided vehicle repair confirmation.</summary>
    public sealed class VehicleRepairWindow : MonoBehaviour
    {
        public GameObject window;
        public TMP_Text title;
        public TMP_Text prompt;
        public TMP_Text rows;
        ClientVehicleRepairs repairs;
        StringTableLocalizer localizer;
        uint displayedPage;
        void Awake()
        {
            repairs = GameServices.Resolve<ClientVehicleRepairs>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
        }
        void OnEnable() { repairs.Changed += Refresh; Refresh(); }
        void OnDisable() => repairs.Changed -= Refresh;
        string Localize(string value) => Regex.Replace(value, @"@[\w/]+:[\w]+", match => localizer.Localize(match.Value));
        void Refresh()
        {
            window.SetActive(repairs.CurrentQuote != null);
            if (repairs.CurrentQuote == null) return;
            var quote = repairs.CurrentQuote;
            title.text = Localize(quote.Title).ToUpperInvariant();
            prompt.text = Localize(quote.Prompt);
            rows.text = string.Join("\n<color=#00adc0>â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€</color>\n", quote.Rows.Select(Localize));
            if (displayedPage != quote.PageId)
            {
                displayedPage = quote.PageId;
                transform.SetAsLastSibling();
                window.transform.SetAsLastSibling();
            }
        }
        public void Accept() => repairs.Respond(true);
        public void Cancel() => repairs.Respond(false);
    }
}
