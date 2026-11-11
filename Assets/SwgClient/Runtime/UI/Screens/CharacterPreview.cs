using Swg.Client.Runtime.Assets.Appearances;
using System;
using System.Collections.Generic;
using UnityEngine;
using Swg.Client.Runtime.World;
using Swg.Client.Core.Characters.Models;
using Swg.Client.Core.Geometry;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Shaders;

namespace Swg.Client.Runtime.UI.Screens
{
    public sealed class CharacterPreview : MonoBehaviour
    {
        const int PreviewLayer = 31;
        public Camera previewCamera;
        public Shader characterShader;
        TreRuntimeAssets assets;
        SwgMaterialFactory materials;
        readonly List<MeshPartResources> resources = new List<MeshPartResources>();
        readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
        GameObject modelRoot;
        [Range(.1f, 1f)]
        public float viewportWidth = 1;
        [Range(.1f, 1f)]
        public float heightFraction = .8f;
        public float yawDegrees = 0;
        public IReadOnlyList<string> Warnings { get; private set; } = Array.Empty<string>();

        public void Show(string key, CachedCharacterModelResult result)
        {
            if (modelRoot != null)
                modelRoot.SetActive(false);
            if (!models.TryGetValue(key, out modelRoot))
            {
                modelRoot = BuildParts(key, result.Parts, result.Sources);
                models.Add(key, modelRoot);
            }

            Warnings = result.Warnings;
            modelRoot.SetActive(true);
            FrameModel(modelRoot);
        }

        public void Initialize(TreRuntimeAssets source)
        {
            assets = source;
            materials = GameServices.Resolve<SwgMaterialFactory>();
        }

        public void Show(string path)
        {
            if (modelRoot != null)
                modelRoot.SetActive(false);
            if (!models.TryGetValue(path, out modelRoot))
            {
                modelRoot = BuildModel(path);
                models.Add(path, modelRoot);
            }

            modelRoot.SetActive(true);
            FrameModel(modelRoot);
        }

        GameObject BuildModel(string path)
        {
            return BuildParts(path, CharacterModel.Load(assets.Loader, path));
        }

        GameObject BuildParts(string path, List<MeshPart> parts, List<CachedMeshPartSource> sources = null)
        {
            var model = new GameObject(path);
            model.transform.SetParent(transform, false);
            model.layer = PreviewLayer;
            model.transform.position = new Vector3(1000, 0, 0);
            model.SetActive(false);
            model.transform.rotation = Quaternion.Euler(0, yawDegrees, 0);
            for (int partIndex = 0; partIndex < parts.Count; partIndex++)
            {
                var part = parts[partIndex];
                var g = new GameObject("Original mesh", typeof(MeshFilter), typeof(MeshRenderer));
                g.transform.SetParent(model.transform, false);
                g.layer = PreviewLayer;
                var shader = characterShader;
                if (shader == null)
                    throw new InvalidOperationException("SWG character shader is missing");
                bool cutout = sources != null && sources[partIndex].ObjectTemplate.StartsWith("object/tangible/hair/", StringComparison.Ordinal);
                var material = materials.Create(shader, part.Diffuse, part.Tint, cutout ? .35f : 0, false, TextureWrapMode.Clamp);
                var resource = new MeshPartResources(path + " part " + partIndex, part, material);
                resources.Add(resource);
                g.GetComponent<MeshFilter>().sharedMesh = resource.Mesh;
                g.GetComponent<MeshRenderer>().sharedMaterial = resource.Material;
            }

            return model;
        }

        void FrameModel(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
                return;
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            previewCamera.rect = previewCamera.targetTexture == null ? new Rect(0, .04f, viewportWidth, .96f) : new Rect(0, 0, 1, 1);
            float distance = Mathf.Max(bounds.size.y, bounds.size.x) / (2 * Mathf.Tan(previewCamera.fieldOfView * .5f * Mathf.Deg2Rad) * heightFraction);
            Vector3 target = bounds.center;
            previewCamera.transform.position = target + new Vector3(-distance * .17f, distance * .03f, distance);
            previewCamera.transform.LookAt(target);
        }

        void OnDestroy()
        {
            foreach (var resource in resources)
                resource.Dispose();
            resources.Clear();
        }
    }
}
