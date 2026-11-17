using System.Collections.Generic;
using Swg.Client.Runtime.UI.Group;
using Swg.Client.Runtime.UI.Interactions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.UI.Windows
{
    /// <summary>Focus follows pointer interactions; Escape invokes the last focused window's existing close action.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class WindowStack : MonoBehaviour
    {
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        RadialContextMenu radial;
        GroupHud group;
        void Awake()
        {
            radial = GetComponentInChildren<RadialContextMenu>(true);
            group = GetComponentInChildren<GroupHud>(true);
        }

        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null && EventSystem.current != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0))
            {
                hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = mouse.position.ReadValue() }, hits);
                if (hits.Count > 0)
                {
                    var window = hits[0].gameObject.GetComponentInParent<UiWindow>();
                    if (window != null) window.Focus();
                }
            }
            // Transient menus consume Escape before their underlying window.
            if (radial != null && radial.IsOpen || group != null && group.menuOverlay.activeInHierarchy) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && Application.isFocused)
            {
                var window = LastWindow();
                if (window != null && UiInput.TryConsumeEscape()) window.closeButton.onClick.Invoke();
            }
        }

        internal static UiWindow LastWindow()
        {
            UiWindow last = null;
            foreach (var window in UiWindow.OpenWindows)
                if (window.closeButton != null && window.closeButton.interactable && (last == null || window.FocusOrder > last.FocusOrder)) last = window;
            return last;
        }
    }
}
