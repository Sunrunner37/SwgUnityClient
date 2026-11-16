using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Windows
{
    /// <summary>Arrow buttons and thumb visibility for the native scrollbar skin; Unity handles scrolling and dragging.</summary>
    public sealed class WindowScrollbar : MonoBehaviour
    {
        public Scrollbar scrollbar;
        public bool hideHandleWhenNotScrollable = true;
        public Button up;
        public Button down;
        public void ScrollUp() => Step(1);
        public void ScrollDown() => Step(-1);
        void Step(float direction) => scrollbar.value = Mathf.Clamp01(scrollbar.value + direction * Mathf.Max(.01f, scrollbar.size * .25f));
        void LateUpdate()
        {
            bool canScroll = scrollbar.size < .999f;
            up.interactable = canScroll;
            down.interactable = canScroll;
            bool showHandle = canScroll || !hideHandleWhenNotScrollable;
            if (scrollbar.handleRect.gameObject.activeSelf != showHandle)
                scrollbar.handleRect.gameObject.SetActive(showHandle);
        }
    }
}
