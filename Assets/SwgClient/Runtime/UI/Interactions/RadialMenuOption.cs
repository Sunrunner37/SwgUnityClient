using Swg.Client.Core.Networking.Game.Packets.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Interactions
{
    public sealed class RadialMenuOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public TMP_Text label;
        RadialContextMenu menu;
        public RadialMenuEntry Entry { get; private set; }
        public void Bind(RadialContextMenu owner, RadialMenuEntry entry, string text)
        {
            menu = owner;
            Entry = entry;
            label.text = text;
            button.onClick.AddListener(Select);
        }
        public void Select() => menu.Select(Entry);
        public void OnPointerEnter(PointerEventData pointer) => menu.Hovered = this;
        public void OnPointerExit(PointerEventData pointer)
        {
            if (menu.Hovered == this)
                menu.Hovered = null;
        }
    }
}
