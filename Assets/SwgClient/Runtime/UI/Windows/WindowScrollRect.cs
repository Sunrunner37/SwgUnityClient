using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Windows
{
    /// <summary>A scrollable HUD surface that moves its window when dragged; wheel and scrollbar behavior stay native.</summary>
    public sealed class WindowScrollRect : ScrollRect
    {
        public UiWindow window;
        public override void OnInitializePotentialDrag(PointerEventData pointer)
        {
            base.OnInitializePotentialDrag(pointer);
            pointer.useDragThreshold = false;
            if (pointer.button == PointerEventData.InputButton.Left) window.BeginDrag(pointer);
        }
        public override void OnBeginDrag(PointerEventData pointer) { }
        public override void OnDrag(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left) window.Drag(pointer, WindowEdges.None);
        }
        public override void OnEndDrag(PointerEventData pointer) { }
    }
}
