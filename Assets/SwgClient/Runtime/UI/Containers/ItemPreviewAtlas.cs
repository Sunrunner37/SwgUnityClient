using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Appearances;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Containers
{
    /// <summary>One camera renders all item thumbnails into an atlas; UI cells reference individual atlas regions.</summary>
    public sealed class ItemPreviewAtlas : MonoBehaviour
    {
        public Camera previewCamera;
        public Transform stage;
        public Vector3 stagePosition = new Vector3(20000, 0, 0);
        public int columns = 9;
        public int pixelsPerItem = 128;
        [Range(-45, 45)]
        public float tiltDegrees = 15;
        public float rotationSeconds = 12;
        public float renderFramesPerSecond = 30;
        readonly List<Transform> pivots = new List<Transform>();
        RenderTexture texture;
        int rows;
        float nextRender;
        void Awake()
        {
            previewCamera.enabled = false;
            // Preview cameras and models live in world units, independent of CanvasScaler.
            stage.SetParent(null, false);
            stage.localScale = Vector3.one;
            stage.position = stagePosition;
        }
        void OnEnable() => stage.gameObject.SetActive(true);
        void OnDisable()
        {
            if (stage != null)
                stage.gameObject.SetActive(false);
        }

        public void Begin(int count)
        {
            Clear();
            for (int i = 0; i < count; i++)
                pivots.Add(null);
            rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            texture = new RenderTexture(columns * pixelsPerItem, rows * pixelsPerItem, 24) { name = "Inventory item atlas", antiAliasing = 2 };
            texture.Create();
            previewCamera.targetTexture = texture;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = rows;
            previewCamera.aspect = columns / (float)rows;
            previewCamera.transform.SetPositionAndRotation(stage.position + new Vector3(-(columns - 1), -(rows - 1), 40), Quaternion.Euler(0, 180, 0));
            nextRender = 0;
        }
        public void Show(int index, AppearanceData appearance, AppearanceResources resources, ContainerItem item) => Show(index, appearance, resources, item.image, "Item " + item.SwgId);

        public void Show(int index, AppearanceData appearance, AppearanceResources resources, RawImage image, string name)
        {
            if (pivots[index] != null)
            {
                pivots[index].gameObject.SetActive(false);
                Destroy(pivots[index].gameObject);
            }
            var pivot = new GameObject(name).transform;
            pivot.SetParent(stage, false);
            pivot.localPosition = new Vector3(-(index % columns) * 2, -(index / columns) * 2, 0);
            var root = resources.Create(pivot, detectMovement: false, previewLighting: true);
            var bounds = new Bounds((appearance.BoundsMin + appearance.BoundsMax) * .5f, appearance.BoundsMax - appearance.BoundsMin);
            float scale = .82f / Mathf.Max(.1f, bounds.extents.magnitude);
            root.transform.localScale = Vector3.one * scale;
            root.transform.localPosition = -bounds.center * scale;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 30;
            pivots[index] = pivot;
            image.texture = texture;
            image.uvRect = new Rect(index % columns / (float)columns, 1 - (index / columns + 1f) / rows, 1f / columns, 1f / rows);
        }
        void LateUpdate()
        {
            if (texture == null || Time.unscaledTime < nextRender)
                return;
            RenderNow();
        }
        public void RenderNow()
        {
            if (texture == null || !isActiveAndEnabled)
                return;
            nextRender = Time.unscaledTime + 1 / renderFramesPerSecond;
            // One complete counter-clockwise turn every twelve seconds, independent of frame rate.
            var rotation = Quaternion.Euler(tiltDegrees, 0, 0) * Quaternion.Euler(0, 45 - Time.unscaledTime * 360 / rotationSeconds, 0);
            foreach (var pivot in pivots)
                if (pivot != null)
                    pivot.localRotation = rotation;
            previewCamera.Render();
        }
        public void Clear()
        {
            foreach (var pivot in pivots)
                if (pivot != null)
                {
                    pivot.gameObject.SetActive(false);
                    Destroy(pivot.gameObject);
                }
            pivots.Clear();
            if (texture != null)
            {
                if (previewCamera != null)
                    previewCamera.targetTexture = null;
                texture.Release();
                Destroy(texture);
                texture = null;
            }
        }
        void OnDestroy()
        {
            Clear();
            if (stage != null)
                Destroy(stage.gameObject);
        }
    }
}
