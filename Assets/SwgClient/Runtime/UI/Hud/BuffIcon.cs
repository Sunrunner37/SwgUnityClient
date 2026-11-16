using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Hud
{
    public sealed class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public RawImage image;
        public double ExpiresAt { get; private set; }
        UiTooltip tooltip;
        string caption;
        public void Bind(string caption, double expiresAt, Texture texture, UiTooltip tooltip)
        {
            this.caption = caption;
            ExpiresAt = expiresAt;
            image.texture = texture;
            this.tooltip = tooltip;
        }
        void Show(PointerEventData pointer)
        {
            double seconds = Math.Max(0, ExpiresAt - Time.realtimeSinceStartupAsDouble);
            var time = TimeSpan.FromSeconds(seconds);
            tooltip.Show(caption + "\n" + (int)time.TotalMinutes + ":" + time.Seconds.ToString("00") + " remaining", pointer);
        }
        public void OnPointerEnter(PointerEventData pointer) => Show(pointer);
        public void OnPointerMove(PointerEventData pointer) => Show(pointer);
        public void OnPointerExit(PointerEventData pointer) => tooltip.Hide();
        void OnDisable() { if (tooltip != null) tooltip.Hide(); }
    }
}
