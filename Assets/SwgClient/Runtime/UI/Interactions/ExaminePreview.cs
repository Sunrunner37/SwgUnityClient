using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Interactions
{
    public sealed class ExaminePreview : MonoBehaviour, IDragHandler
    {
        public Vector3 stagePosition = new Vector3(10000, 0, 0);
        public Vector3 modelRotation;
        public RawImage image;
        public Camera previewCamera;
        public Transform stage;
        public Shader worldShader;
        public Shader characterShader;
        public Shader particleShader;
        AppearanceResources resources;
        bool ownsResources;
        GameObject model;
        RenderTexture texture;
        Bounds modelBounds;
        void Awake()
        {
            texture = new RenderTexture(512, 512, 24) { antiAliasing = 4, name = "Examine preview" };
            texture.Create();
            image.texture = texture;
            image.enabled = false;
            previewCamera.targetTexture = texture;
            previewCamera.enabled = false;
            // Keep the 3D stage out of the scaled UI hierarchy.
            stage.SetParent(null, false);
            stage.localScale = Vector3.one;
            stage.position = stagePosition;
        }
        void OnEnable()
        {
            stage.gameObject.SetActive(true);
            previewCamera.enabled = model != null;
        }
        void OnDisable()
        {
            if (previewCamera != null)
                previewCamera.enabled = false;
            if (stage != null)
                stage.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            var size = image.rectTransform.rect.size;
            float aspect = Mathf.Max(1, size.x) / Mathf.Max(1, size.y);
            int width = Mathf.Clamp(Mathf.RoundToInt(512 * aspect), 64, 2048);
            if (texture.width != width)
            {
                previewCamera.targetTexture = null;
                texture.Release();
                texture.width = width;
                texture.Create();
                previewCamera.targetTexture = texture;
            }
            if (!Mathf.Approximately(previewCamera.aspect, aspect))
            {
                previewCamera.aspect = aspect;
                if (model != null) FrameModel();
            }
        }
        public void Show(AppearanceData appearance, AppearanceResources sharedResources = null)
        {
            Clear();
            ownsResources = sharedResources == null;
            resources = sharedResources ?? new AppearanceResources(appearance, GameServices.Resolve<SwgMaterialFactory>(), worldShader, characterShader, particleShader);
            model = resources.Create(stage, detectMovement: false, previewLighting: true);
            model.transform.localRotation = Quaternion.Euler(modelRotation);
            foreach (var child in model.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 31;
            modelBounds = new Bounds((appearance.BoundsMin + appearance.BoundsMax) * .5f, appearance.BoundsMax - appearance.BoundsMin);
            FrameModel();
            previewCamera.enabled = true;
            image.enabled = true;
        }
        void FrameModel()
        {
            float radius = Mathf.Max(.1f, modelBounds.extents.magnitude);
            var center = model.transform.TransformPoint(modelBounds.center);
            float distance = radius / Mathf.Sin(previewCamera.fieldOfView * Mathf.Deg2Rad * .5f) * 1.15f / Mathf.Min(1, previewCamera.aspect);
            previewCamera.transform.position = center + new Vector3(-.25f, .15f, 1).normalized * distance;
            previewCamera.transform.LookAt(center);
            previewCamera.farClipPlane = distance + radius * 3;
        }
        public void OnDrag(PointerEventData pointer)
        {
            if (model != null)
                model.transform.Rotate(Vector3.up, -pointer.delta.x * .5f, Space.World);
        }
        public void Clear()
        {
            if (image != null)
                image.enabled = false;
            if (previewCamera != null)
                previewCamera.enabled = false;
            if (model != null)
            {
                model.SetActive(false);
                Destroy(model);
            }
            if (ownsResources)
                resources?.Dispose();
            resources = null;
        }
        void OnDestroy()
        {
            Clear();
            if (stage != null)
                Destroy(stage.gameObject);
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
