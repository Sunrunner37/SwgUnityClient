using NUnit.Framework;
using Swg.Client.Runtime.UI.Chat;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Runtime.UI.Theming;
using Swg.Client.Runtime.UI.Windows;

namespace Swg.Client.Tests.Editor
{
    public sealed class ChatUiChecks
    {
        [Test]
        public void WindowCloseFollowsInteractionAndInvokesItsExistingAction()
        {
            var parent = new GameObject("Window stack", typeof(RectTransform));
            UiWindow Add(string name)
            {
                var root = new GameObject(name, typeof(RectTransform), typeof(UiWindow));
                root.transform.SetParent(parent.transform);
                var window = root.GetComponent<UiWindow>();
                // Edit Mode does not run this MonoBehaviour lifecycle automatically.
                typeof(UiWindow).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(window, null);
                window.closeButton = new GameObject("Close", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
                window.closeButton.transform.SetParent(root.transform);
                window.closeButton.onClick.AddListener(() => { root.SetActive(false); typeof(UiWindow).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(window, null); });
                return window;
            }
            try
            {
                var first = Add("Inventory");
                var second = Add("Datapad");
                Assert.That(WindowStack.LastWindow(), Is.SameAs(second));
                first.Focus();
                Assert.That(WindowStack.LastWindow(), Is.SameAs(first));
                WindowStack.LastWindow().closeButton.onClick.Invoke();
                Assert.That(first.gameObject.activeSelf, Is.False);
                Assert.That(second.gameObject.activeSelf, Is.True);
                Assert.That(WindowStack.LastWindow(), Is.SameAs(second));
            }
            finally
            {
                foreach (var window in parent.GetComponentsInChildren<UiWindow>(true))
                    typeof(UiWindow).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(window, null);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void AllClosableHudWindowsUseTheirPrefabCloseAction()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab");
            Assert.That(hud.GetComponent<WindowStack>(), Is.Not.Null);
            foreach (var window in hud.GetComponentsInChildren<UiWindow>(true))
                foreach (var button in window.GetComponentsInChildren<Button>(true))
                    if (button.name == "Close") Assert.That(window.closeButton, Is.Not.Null, window.name);
        }

        [Test]
        public void ChatIdentityAndSendDestinationDoNotDependOnHierarchyNames()
        {
            const string path = "Assets/Prefabs/UI/Chat/ChatWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var chat = root.GetComponent<ChatWindow>();
                var tab = chat.tabs[0];
                tab.toggle.name = "Renamed in editor";
                chat.SelectTab(0);
                Assert.That(chat.SelectedTabId, Is.EqualTo(tab.id));
                string destination = null;
                chat.onSubmit.AddListener((channel, text) => destination = channel);
                tab.sendChannel = "Independent destination";
                chat.Submit("Hello");
                Assert.That(destination, Is.EqualTo(tab.sendChannel));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ChatPrefabHasExplicitStylingAndPersistentShortcutActions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Chat/ChatWindow.prefab");
            var chat = prefab.GetComponent<ChatWindow>();
            Assert.That(AssetDatabase.Contains(chat.openChat), Is.True);
            Assert.That(AssetDatabase.Contains(chat.releaseChat), Is.True);
            Assert.That(chat.openChat.action.actionMap.name, Is.EqualTo("Chat"));
            Assert.That(chat.releaseChat.action.actionMap.name, Is.EqualTo("Chat"));
            var theme = prefab.GetComponent<UiThemeBinding>();
            Assert.That(theme.onApplied.GetPersistentEventCount(), Is.Zero);
            foreach (var tab in chat.tabs)
            {
                var styling = tab.toggle.GetComponent<ThemedToggle>();
                Assert.That(styling.theme, Is.SameAs(theme));
                Assert.That(styling.toggle, Is.SameAs(tab.toggle));
                Assert.That(styling.label, Is.Not.Null);
                Assert.That(theme.colors.Exists(binding => binding.graphic == styling.label), Is.False);
                Assert.That(theme.texts.Exists(binding => binding.text == styling.label), Is.True);
            }
        }

        [Test]
        public void WindowHandlesApplyTheFirstMovementAfterPointerDown()
        {
            var parent = new GameObject("Canvas", typeof(RectTransform));
            var root = new GameObject("Window", typeof(RectTransform), typeof(UiWindow));
            var grip = new GameObject("Grip", typeof(RectTransform), typeof(WindowHandle));
            try
            {
                ((RectTransform)parent.transform).sizeDelta = new Vector2(1000, 600);
                var rect = (RectTransform)root.transform;
                rect.SetParent(parent.transform, false);
                rect.sizeDelta = new Vector2(500, 200);
                grip.transform.SetParent(rect, false);
                var handle = grip.GetComponent<WindowHandle>();
                handle.window = root.GetComponent<UiWindow>();
                var pointer = new PointerEventData(null)
                {
                    position = Vector2.zero,
                    button = PointerEventData.InputButton.Left
                };
                Assert.That(ExecuteEvents.Execute(grip, pointer, ExecuteEvents.pointerDownHandler), Is.True);
                pointer.position = new Vector2(50, 40);
                ExecuteEvents.Execute(grip, pointer, ExecuteEvents.dragHandler);
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(50, 40)));
                handle.edges = WindowEdges.Right | WindowEdges.Top;
                ExecuteEvents.Execute(grip, pointer, ExecuteEvents.pointerDownHandler);
                pointer.position += new Vector2(60, 30);
                ExecuteEvents.Execute(grip, pointer, ExecuteEvents.dragHandler);
                Assert.That(rect.rect.size, Is.EqualTo(new Vector2(560, 230)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void HudInputActionsAreSavedAssetReferences()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab");
            var module = System.Array.Find(hud.GetComponentsInChildren<MonoBehaviour>(true), component => component.GetType().Name == "InputSystemUIInputModule");
            var serialized = new SerializedObject(module);
            foreach (string field in new[]
            {
                "m_ActionsAsset",
                "m_PointAction",
                "m_LeftClickAction",
                "m_MoveAction",
                "m_SubmitAction",
                "m_CancelAction",
                "m_ScrollWheelAction"
            }

            )
            {
                var reference = serialized.FindProperty(field).objectReferenceValue;
                Assert.That(reference, Is.Not.Null, field);
                Assert.That(AssetDatabase.Contains(reference), Is.True, field);
            }
        }

        [Test]
        public void QualifiedStylesDisambiguateRepeatedNames()
        {
            var root = UiNode.Parse("<Page Name='Styles'><Page Name='chat'><Style Name='default'/></Page><Page Name='scrollbar'><Style Name='default'/></Page></Page>");
            var chat = root.FindPath("Styles.chat.default");
            var scrollbar = root.FindPath("Styles/scrollbar/default");
            Assert.That(chat, Is.Not.Null);
            Assert.That(scrollbar, Is.Not.Null.And.Not.SameAs(chat));
            Assert.That(root.FindPath("Styles.missing.default"), Is.Null);
        }

        [Test]
        public void WindowKeepsOppositeCornerAndClampsToMinimumAndParent()
        {
            var parent = new GameObject("Canvas", typeof(RectTransform));
            var root = new GameObject("Window", typeof(RectTransform), typeof(UiWindow));
            try
            {
                var bounds = (RectTransform)parent.transform;
                bounds.sizeDelta = new Vector2(1000, 600);
                var rect = (RectTransform)root.transform;
                rect.SetParent(bounds, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(100, 100);
                rect.sizeDelta = new Vector2(500, 200);
                var window = root.GetComponent<UiWindow>();
                var pointer = new PointerEventData(null)
                {
                    position = Vector2.zero
                };
                window.BeginDrag(pointer);
                window.Resize(new Vector2(-1000, -1000), WindowEdges.Top | WindowEdges.Right);
                Assert.That(rect.rect.size, Is.EqualTo(window.minimumSize));
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(100, 100)));
                window.BeginDrag(pointer);
                window.Resize(new Vector2(2000, 2000), WindowEdges.None);
                Assert.That(rect.anchoredPosition, Is.EqualTo(bounds.rect.size - rect.rect.size));
                window.BeginDrag(pointer);
                window.Resize(new Vector2(-2000, -2000), WindowEdges.None);
                Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ThemeAppliedWhileInactiveSubscribesWhenEnabledAndDetachesWhenSwapped()
        {
            var first = ScriptableObject.CreateInstance<UiTheme>();
            var second = ScriptableObject.CreateInstance<UiTheme>();
            var root = new GameObject("Themed window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.SetActive(false);
            try
            {
                var image = root.GetComponent<Image>();
                var binding = root.AddComponent<UiThemeBinding>();
                binding.theme = first;
                binding.colors.Add(new UiThemeBinding.Binding { graphic = image, role = UiThemeBinding.Role.Accent });
                binding.Apply();
                root.SetActive(true);
                first.accent = Color.green;
                first.NotifyChanged();
                Assert.That(image.color, Is.EqualTo(Color.green));
                binding.theme = second;
                binding.Apply();
                first.accent = Color.red;
                first.NotifyChanged();
                Assert.That(image.color, Is.EqualTo(second.accent));
                second.accent = Color.blue;
                second.NotifyChanged();
                Assert.That(image.color, Is.EqualTo(Color.blue));
                root.SetActive(false);
                second.accent = Color.yellow;
                second.NotifyChanged();
                Assert.That(image.color, Is.EqualTo(Color.blue));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
