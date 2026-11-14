using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swg.Client.Runtime.UI
{
    /// <summary>Clickable TMP links with an underline on the link currently under the pointer.</summary>
    public sealed class UiTextLinks : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
    {
        public TMP_Text label;
        public event Action<string> Clicked;
        string markup;
        string hovered;
        public void SetText(string text) { markup = text; hovered = null; label.text = text; }
        string LinkAt(PointerEventData pointer)
        {
            int index = TMP_TextUtilities.FindIntersectingLink(label, pointer.position, pointer.enterEventCamera);
            return index < 0 ? null : label.textInfo.linkInfo[index].GetLinkID();
        }
        void Hover(string link)
        {
            if (hovered == link) return;
            hovered = link;
            string text = markup;
            if (link != null)
            {
                string tag = "<link=\"" + link + "\">";
                int start = text.IndexOf(tag, StringComparison.Ordinal) + tag.Length;
                int end = text.IndexOf("</link>", start, StringComparison.Ordinal);
                text = text.Insert(end, "</u>").Insert(start, "<u>");
            }
            label.text = text;
        }
        public void OnPointerMove(PointerEventData pointer) => Hover(LinkAt(pointer));
        public void OnPointerExit(PointerEventData pointer) => Hover(null);
        public void OnPointerClick(PointerEventData pointer)
        {
            if (pointer.button != PointerEventData.InputButton.Left) return;
            string link = LinkAt(pointer);
            if (link != null) Clicked?.Invoke(link);
        }
        void OnDisable() => Hover(null);
    }
}
