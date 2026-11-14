using Swg.Client.Runtime.Assets.Appearances;
using System;
using System.Collections.Generic;
using UnityEngine;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Shaders;

namespace Swg.Client.Runtime.UI.Screens
{
    public sealed class CharacterPreview : MonoBehaviour
    {
        const int PreviewLayer = 31;
        sealed class Model
        {
            public GameObject Root;
            public AppearanceResources Resources;
            public Bounds Bounds;
        }

        public Camera previewCamera;
        public Shader characterShader;
        TreRuntimeAssets assets;
        SwgMaterialFactory materials;
        readonly Dictionary<string, Model> models = new Dictionary<string, Model>();
        GameObject modelRoot;
        [Range(.1f, 1f)]
        public float viewportWidth = 1;
        [Range(.1f, 1f)]
        public float heightFraction = .8f;
        public float yawDegrees = 0;
        public void Show(string key, SkeletalAppearanceData result)
        {
            if (!models.TryGetValue(key, out var model))
            {
                model = BuildModel(key, result);
                models.Add(key, model);
            }

            Show(model);
        }

        public void Initialize(TreRuntimeAssets source)
        {
            assets = source;
            materials = GameServices.Resolve<SwgMaterialFactory>();
        }

        public void Show(string path)
        {
            if (!models.TryGetValue(path, out var model))
            {
                model = BuildModel(path, SkeletalAppearanceLoader.LoadDetailed(assets.Loader, path));
                models.Add(path, model);
            }

            Show(model);
        }

        void Show(Model model)
        {
            if (modelRoot != null)
                modelRoot.SetActive(false);
            modelRoot = model.Root;
            modelRoot.SetActive(true);
            FrameModel(modelRoot.transform, model.Bounds);
        }

        Model BuildModel(string key, SkeletalAppearanceData result)
        {
            if (characterShader == null)
                throw new InvalidOperationException("SWG character shader is missing");
            var appearance = new AppearanceData
            {
                Key = key,
                Parts = result.Parts
            };
            appearance.RecalculateBounds();
            var resources = new AppearanceResources(appearance, materials, characterShader, characterShader, null, TextureWrapMode.Clamp);
            var root = resources.Create(transform, detectMovement: false);
            root.SetActive(false);
            root.transform.SetPositionAndRotation(new Vector3(1000, 0, 0), Quaternion.Euler(0, yawDegrees, 0));
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = PreviewLayer;
            return new Model
            {
                Root = root,
                Resources = resources,
                Bounds = new Bounds((appearance.BoundsMin + appearance.BoundsMax) * .5f, appearance.BoundsMax - appearance.BoundsMin)
            };
        }

        void FrameModel(Transform model, Bounds localBounds)
        {
            // Use the neutral appearance bounds, not the expanded animation culling bounds.
            var bounds = new Bounds(model.TransformPoint(localBounds.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
                bounds.Encapsulate(model.TransformPoint(new Vector3((corner & 1) == 0 ? localBounds.min.x : localBounds.max.x, (corner & 2) == 0 ? localBounds.min.y : localBounds.max.y, (corner & 4) == 0 ? localBounds.min.z : localBounds.max.z)));
            previewCamera.rect = previewCamera.targetTexture == null ? new Rect(0, .04f, viewportWidth, .96f) : new Rect(0, 0, 1, 1);
            float distance = Mathf.Max(bounds.size.y, bounds.size.x) / (2 * Mathf.Tan(previewCamera.fieldOfView * .5f * Mathf.Deg2Rad) * heightFraction);
            Vector3 target = bounds.center;
            previewCamera.transform.position = target + new Vector3(-distance * .17f, distance * .03f, distance);
            previewCamera.transform.LookAt(target);
        }

        void OnDestroy()
        {
            foreach (var model in models.Values)
                model.Resources.Dispose();
            models.Clear();
        }
    }
}
