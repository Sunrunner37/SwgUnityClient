using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Windows
{
    /// <summary>Arrow buttons and thumb visibility for the native scrollbar skin; Unity handles scrolling and dragging.</summary>
    public sealed class WindowScrollbar : MonoBehaviour
    {
        public Scrollbar scrollbar;
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
            if (scrollbar.handleRect.gameObject.activeSelf != canScroll)
                scrollbar.handleRect.gameObject.SetActive(canScroll);
        }
    }
}
