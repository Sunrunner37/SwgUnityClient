using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Skills
{
    public sealed class SkillsDetailRow : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public Image icon;
        public TMP_Text label;
        public TMP_Text value;
        UiTooltip tooltip;
        string description;
        public void Bind(string name, string amount, Sprite sprite, string help, UiTooltip tooltip)
        {
            label.text = name;
            value.text = amount;
            value.gameObject.SetActive(!string.IsNullOrEmpty(amount));
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            this.tooltip = tooltip;
            description = help;
        }
        public void OnPointerEnter(PointerEventData pointer) { if (!string.IsNullOrEmpty(description)) tooltip.Show(description, pointer); }
        public void OnPointerMove(PointerEventData pointer) { if (!string.IsNullOrEmpty(description) && tooltip.gameObject.activeSelf) tooltip.Move(pointer); }
        public void OnPointerExit(PointerEventData pointer) => tooltip.Hide();
        void OnDisable() { if (tooltip != null) tooltip.Hide(); }
    }
}
