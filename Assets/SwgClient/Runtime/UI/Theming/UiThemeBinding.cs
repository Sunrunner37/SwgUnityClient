using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Theming
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class UiThemeBinding : MonoBehaviour
    {
        public enum Role
        {
            Accent,
            Text,
            Surface,
            Input,
            Tab,
            OnAccent,
            Highlight
        }

        [Serializable]
        public struct Binding
        {
            public Graphic graphic;
            public Role role;
        }

        [Serializable]
        public struct TextBinding
        {
            public TMP_Text text;
            public float sizeOffset;
        }

        public UiTheme theme;
        public List<Binding> colors = new List<Binding>();
        public List<TextBinding> texts = new List<TextBinding>();
        public List<TMP_InputField> inputs = new List<TMP_InputField>();
        public UnityEvent onApplied = new UnityEvent();
        UiTheme subscribed;
        void OnEnable() => Apply();
        void OnDisable()
        {
            if (subscribed != null)
                subscribed.Changed -= Apply;
            subscribed = null;
        }

        void OnValidate() => Apply();
        public void Apply()
        {
            var activeTheme = isActiveAndEnabled ? theme : null;
            if (subscribed != activeTheme)
            {
                if (subscribed != null)
                    subscribed.Changed -= Apply;
                subscribed = activeTheme;
                if (subscribed != null && isActiveAndEnabled)
                    subscribed.Changed += Apply;
            }

            if (theme == null)
                return;
            foreach (var binding in colors)
                if (binding.graphic != null)
                    binding.graphic.color = ColorFor(binding.role);
            foreach (var binding in texts)
            {
                if (binding.text == null)
                    continue;
                if (theme.font != null)
                    binding.text.font = theme.font;
                binding.text.fontSize = theme.fontSize + binding.sizeOffset;
            }

            foreach (var input in inputs)
            {
                if (input == null)
                    continue;
                input.caretColor = theme.text;
                input.selectionColor = new Color(theme.accent.r, theme.accent.g, theme.accent.b, .3f);
            }

            onApplied.Invoke();
        }

        Color ColorFor(Role role) => role switch
        {
            Role.Accent => theme.accent,
            Role.Text => theme.text,
            Role.Surface => theme.surface,
            Role.Input => theme.input,
            Role.Tab => theme.tab,
            Role.OnAccent => theme.onAccent,
            Role.Highlight => theme.highlight,
            _ => theme.text
        };
    }
}
