using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Theming
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ThemedToggle : MonoBehaviour
    {
        public UiThemeBinding theme;
        public Toggle toggle;
        public TMP_Text label;
        void OnEnable()
        {
            if (toggle != null)
                toggle.onValueChanged.AddListener(OnSelectionChanged);
            if (theme != null)
                theme.onApplied.AddListener(Apply);
            Apply();
        }

        void OnDisable()
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnSelectionChanged);
            if (theme != null)
                theme.onApplied.RemoveListener(Apply);
        }

        void OnValidate()
        {
            if (theme != null && toggle != null && label != null)
                Apply();
        }

        void OnSelectionChanged(bool selected) => Apply();
        public void Apply()
        {
            if (theme != null && theme.theme != null && toggle != null && label != null)
                label.color = toggle.isOn ? theme.theme.onAccent : theme.theme.text;
        }
    }
}
