using System.Collections.Generic;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.SceneObjects;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Projects server fly text above rendered objects, drifting upward and fading out.</summary>
    public sealed class FlyTextHud : MonoBehaviour, IUiPrefabSource
    {
        sealed class Entry
        {
            public TMP_Text Label;
            public Transform Target;
            public float Height;
            public float Age;
            public float Offset;
        }
        public TMP_Text textPrefab;
        public float duration = 2.5f;
        public float rise = 65;
        public IEnumerable<GameObject> UiPrefabs { get { yield return textPrefab.gameObject; } }
        readonly List<Entry> entries = new List<Entry>();
        ClientFlyText source;
        WorldRenderingContext rendering;
        RectTransform area;
        Canvas canvas;
        void Awake()
        {
            source = GameServices.Resolve<ClientFlyText>();
            rendering = GameServices.Resolve<WorldRenderingContext>();
            area = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
        }
        void OnEnable() => source.Received += Show;
        void OnDisable()
        {
            source.Received -= Show;
            foreach (var entry in entries)
                Destroy(entry.Label.gameObject);
            entries.Clear();
        }
        public void Show(ClientFlyText.Message message)
        {
            if (!ManagedSceneObject.TryGetObjectFrom(message.SwgId, out var target) || string.IsNullOrEmpty(message.Text))
                return;
            var label = Instantiate(textPrefab, transform);
            label.text = message.Text;
            label.color = message.Color;
            label.fontSize *= message.Scale;
            float height = target.TryGetComponent<TargetableSceneObject>(out var pick) ? pick.LocalBounds.max.y : 2;
            int stacked = entries.FindAll(entry => entry.Target == target.transform).Count;
            entries.Add(new Entry { Label = label, Target = target.transform, Height = height, Offset = stacked * label.fontSize });
        }
        void LateUpdate()
        {
            var camera = rendering.WorldCamera;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                entry.Age += Time.unscaledDeltaTime;
                if (entry.Target == null || entry.Age >= duration)
                {
                    Destroy(entry.Label.gameObject);
                    entries.RemoveAt(i);
                    continue;
                }
                if (camera == null)
                    continue;
                var screen = camera.WorldToScreenPoint(entry.Target.TransformPoint(Vector3.up * entry.Height));
                entry.Label.enabled = screen.z > 0;
                var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, uiCamera, out var local);
                entry.Label.rectTransform.anchoredPosition = local + Vector2.up * (15 + entry.Offset + rise * entry.Age / duration);
                entry.Label.alpha = Mathf.Clamp01((duration - entry.Age) / .5f);
            }
        }
    }
}
