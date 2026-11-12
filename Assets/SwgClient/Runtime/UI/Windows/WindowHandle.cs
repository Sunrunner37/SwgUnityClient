using UnityEngine;
using UnityEngine.EventSystems;

namespace Swg.Client.Runtime.UI.Windows
{
    public sealed class WindowHandle : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler
    {
        public UiWindow window;
        [Tooltip("None moves the window; select edges to resize it.")]
        public WindowEdges edges;
        public void OnInitializePotentialDrag(PointerEventData pointer) => pointer.useDragThreshold = false;
        public void OnPointerDown(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left)
                window.BeginDrag(pointer);
        }

        public void OnDrag(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left)
                window.Drag(pointer, edges);
        }
    }
}
