using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Chat
{
    public sealed class ChatWindow : MonoBehaviour
    {
        [Serializable]
        public sealed class SubmitEvent : UnityEvent<string, string>
        {
        }

        [Serializable]
        public sealed class Tab
        {
            public string id;
            public string sendChannel;
            public Toggle toggle;
        }

        public List<Tab> tabs = new List<Tab>();
        public InputActionReference openChat;
        public InputActionReference releaseChat;
        public ScrollRect tabScroll;
        public ScrollRect messages;
        public TextMeshProUGUI output;
        public TMP_InputField input;
        public TextMeshProUGUI channelLabel;
        [Min(1)]
        public int historyLimit = 500;
        public SubmitEvent onSubmit = new SubmitEvent();
        readonly Dictionary<string, Queue<string>> history = new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);
        readonly List<UnityAction<bool>> tabListeners = new List<UnityAction<bool>>();
        Tab selectedTab;
        bool dirty;
        bool followBottom;
        int consumedFrame;
        public string SelectedTabId => selectedTab.id;

        void OnEnable()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                UnityAction<bool> callback = active =>
                {
                    if (active)
                        SelectTab(index);
                };
                tabListeners.Add(callback);
                tabs[i].toggle.onValueChanged.AddListener(callback);
            }

            openChat.action.Enable();
            releaseChat.action.Enable();
            input.onSubmit.AddListener(Submit);
            int selected = tabs.FindIndex(tab => tab.toggle.isOn);
            if (selected >= 0)
                SelectTab(selected);
        }

        void OnDisable()
        {
            for (int i = 0; i < tabListeners.Count; i++)
                tabs[i].toggle.onValueChanged.RemoveListener(tabListeners[i]);
            tabListeners.Clear();
            input.onSubmit.RemoveListener(Submit);
            openChat.action.Disable();
            releaseChat.action.Disable();
        }

        void Update()
        {
            if (!Application.isFocused || consumedFrame == Time.frameCount)
                return;
            if (input.isFocused && releaseChat.action.WasPressedThisFrame())
                ReleaseInput();
            else if (!UiInput.TextInputFocused && openChat.action.WasPressedThisFrame())
            {
                input.Select();
                input.ActivateInputField();
            }
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count)
                return;
            if (!tabs[index].toggle.isOn)
            {
                tabs[index].toggle.isOn = true;
                return;
            }

            selectedTab = tabs[index];
            channelLabel.text = selectedTab.sendChannel;
            dirty = true;
            followBottom = true;
        }

        public void ScrollTabs(float amount) => tabScroll.horizontalNormalizedPosition = Mathf.Clamp01(tabScroll.horizontalNormalizedPosition + amount);
        public void PreviousTabs() => ScrollTabs(-.35f);
        public void NextTabs() => ScrollTabs(.35f);
        public void NextChannel() => SelectTab((tabs.FindIndex(tab => tab.toggle.isOn) + 1) % tabs.Count);
        public void ScrollUp() => ScrollMessages(1);
        public void ScrollDown() => ScrollMessages(-1);
        void ScrollMessages(float direction)
        {
            float overflow = messages.content.rect.height - messages.viewport.rect.height;
            if (overflow > 0)
                messages.verticalNormalizedPosition = Mathf.Clamp01(messages.verticalNormalizedPosition + direction * output.fontSize * 3 / overflow);
        }

        public void ClearHistory()
        {
            history.Clear();
            dirty = true;
            followBottom = true;
        }

        public void AppendMessage(string tabId, string text)
        {
            if (!history.TryGetValue(tabId, out var lines))
                history.Add(tabId, lines = new Queue<string>());
            lines.Enqueue(text);
            while (lines.Count > historyLimit)
                lines.Dequeue();
            if (selectedTab == null || !string.Equals(selectedTab.id, tabId, StringComparison.OrdinalIgnoreCase))
                return;
            followBottom |= messages.content.rect.height <= messages.viewport.rect.height || messages.verticalNormalizedPosition <= .02f;
            dirty = true;
        }

        void LateUpdate()
        {
            if (!dirty)
                return;
            output.text = history.TryGetValue(selectedTab.id, out var lines) ? string.Join("\n", lines) : "";
            LayoutRebuilder.ForceRebuildLayoutImmediate(messages.content);
            if (followBottom)
                messages.verticalNormalizedPosition = 0;
            dirty = false;
            followBottom = false;
        }

        public void Submit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                onSubmit.Invoke(selectedTab.sendChannel, text);
            input.SetTextWithoutNotify("");
            ReleaseInput();
        }

        void ReleaseInput()
        {
            consumedFrame = Time.frameCount;
            input.DeactivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
