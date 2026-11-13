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
        void Awake()
        {
            texture = new RenderTexture(512, 512, 24) { antiAliasing = 4, name = "Examine preview" };
            texture.Create();
            image.texture = texture;
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
        public void Show(AppearanceData appearance, AppearanceResources sharedResources = null)
        {
            Clear();
            ownsResources = sharedResources == null;
            resources = sharedResources ?? new AppearanceResources(appearance, GameServices.Resolve<SwgMaterialFactory>(), worldShader, characterShader, particleShader);
            model = resources.Create(stage, detectMovement: false);
            foreach (var child in model.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 31;
            var bounds = new Bounds((appearance.BoundsMin + appearance.BoundsMax) * .5f, appearance.BoundsMax - appearance.BoundsMin);
            float radius = Mathf.Max(.1f, bounds.extents.magnitude);
            var center = model.transform.TransformPoint(bounds.center);
            float distance = radius / Mathf.Sin(previewCamera.fieldOfView * Mathf.Deg2Rad * .5f) * 1.15f;
            previewCamera.transform.position = center + new Vector3(-.25f, .15f, 1).normalized * distance;
            previewCamera.transform.LookAt(center);
            previewCamera.farClipPlane = distance + radius * 3;
            previewCamera.enabled = true;
        }
        public void OnDrag(PointerEventData pointer)
        {
            if (model != null)
                model.transform.Rotate(Vector3.up, -pointer.delta.x * .5f, Space.World);
        }
        public void Clear()
        {
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
