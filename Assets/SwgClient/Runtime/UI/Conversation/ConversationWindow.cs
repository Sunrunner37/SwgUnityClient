using System.Collections.Generic;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Theming;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.SceneObjects;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Conversation
{
    /// <summary>NPC-anchored speech and response buttons. Layout and artwork are owned by the prefab.</summary>
    public sealed class ConversationWindow : MonoBehaviour, IUiPrefabSource
    {
        public RectTransform panel;
        public RectTransform bubble;
        public TMP_Text message;
        public TMP_Text speaker;
        public ScrollRect responses;
        public Button optionPrefab;
        public Button stop;
        public UiThemeBinding theme;
        public float maximumResponseHeight = 300;
        public Vector2 screenOffset = new Vector2(0, 12);
        public IEnumerable<GameObject> UiPrefabs { get { yield return optionPrefab.gameObject; } }
        readonly List<Button> rows = new List<Button>();
        IReadOnlyList<string> renderedOptions;
        StringTableLocalizer localizer;
        ClientConversation conversation;
        SceneObjectNameResolver names;
        WorldRenderingContext rendering;
        WorldState state;
        Canvas canvas;
        bool wasOpen;

        void Awake()
        {
            conversation = GameServices.Resolve<ClientConversation>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
            rendering = GameServices.Resolve<WorldRenderingContext>();
            state = GameServices.Resolve<WorldState>();
            canvas = GetComponentInParent<Canvas>();
            panel.gameObject.SetActive(false);
            stop.onClick.AddListener(conversation.Stop);
        }
        void OnEnable()
        {
            conversation.Changed += Refresh;
            Refresh();
        }
        void OnDisable() => conversation.Changed -= Refresh;
        void Refresh()
        {
            panel.gameObject.SetActive(conversation.IsOpen);
            if (!conversation.IsOpen) { wasOpen = false; return; }
            if (!wasOpen) transform.SetAsLastSibling();
            wasOpen = true;
            message.text = conversation.Message;
            speaker.text = state.SceneObjects.TryGet(conversation.NpcSwgId, out var npc) ? names.Resolve(npc) : "";
            if (!ReferenceEquals(renderedOptions, conversation.Options))
            {
                renderedOptions = conversation.Options;
                foreach (var row in rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
                rows.Clear();
                for (int index = 0; index < conversation.Options.Count; index++)
                {
                    int choice = index;
                    var row = Instantiate(optionPrefab, responses.content);
                    row.GetComponentInChildren<TMP_Text>().text = conversation.Options[index];
                    row.onClick.AddListener(() => conversation.Select(choice));
                    row.gameObject.SetActive(true);
                    rows.Add(row);
                }
            }
            foreach (var row in rows) row.interactable = !conversation.Waiting;
            stop.transform.SetAsLastSibling();
            var stopCaption = localizer.Lookup("ui_radial", "converse_stop");
            stop.GetComponentInChildren<TMP_Text>().text = stopCaption;
            LayoutRebuilder.ForceRebuildLayoutImmediate(responses.content);
            responses.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Min(maximumResponseHeight, LayoutUtility.GetPreferredHeight(responses.content)));
            float verticalPadding = bubble.rect.height - message.rectTransform.rect.height;
            bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(LayoutUtility.GetMinHeight(bubble), message.GetPreferredValues(message.text, message.rectTransform.rect.width, 0).y + verticalPadding));
            responses.verticalNormalizedPosition = 1;
        }
        void LateUpdate()
        {
            if (!conversation.IsOpen) return;
            var area = (RectTransform)transform;
            Vector2 point = area.rect.center + new Vector2(0, area.rect.height * .15f);
            var camera = rendering.WorldCamera;
            if (camera != null && ManagedSceneObject.TryGetObjectFrom(conversation.NpcSwgId, out var npc))
            {
                var top = npc.transform.position + Vector3.up * 1.8f;
                if (npc.TryGetComponent<TargetableSceneObject>(out var target))
                    top = npc.transform.TransformPoint(target.LocalBounds.center + Vector3.up * target.LocalBounds.extents.y);
                var projected = camera.WorldToScreenPoint(top);
                panel.gameObject.SetActive(projected.z > 0);
                if (projected.z <= 0) return;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, projected,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out point);
            }
            point += screenOffset;
            float height = Mathf.Max(bubble.rect.height, ((RectTransform)responses.transform).rect.height);
            point.x = Mathf.Clamp(point.x, area.rect.xMin + panel.rect.width * .5f + 8, area.rect.xMax - panel.rect.width * .5f - 8);
            point.y = Mathf.Clamp(point.y, area.rect.yMin + height + 8, area.rect.yMax - 24);
            panel.anchoredPosition = point - area.rect.center;
        }
    }
}
