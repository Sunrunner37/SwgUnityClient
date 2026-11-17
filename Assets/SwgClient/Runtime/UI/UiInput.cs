using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI
{
    public static class UiInput
    {
        static int escapeFrame = -1;
        public static bool TryConsumeEscape()
        {
            if (!Application.isFocused || escapeFrame == Time.frameCount || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return false;
            escapeFrame = Time.frameCount;
            return true;
        }

        public static bool TextInputFocused
        {
            get
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected == null)
                    return false;
                return selected.TryGetComponent<TMP_InputField>(out var tmp) && tmp.isFocused || selected.TryGetComponent<InputField>(out var input) && input.isFocused;
            }
        }

        public static bool PointerOverUi => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
