using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI
{
    public static class UiInput
    {
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
