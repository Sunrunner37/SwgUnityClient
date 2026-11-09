using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UObj = UnityEngine.Object;
using Swg.Client.Core.Characters.Models;
using Swg.Client.Core.Geometry;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.UI.Screens
{
    public sealed class CharacterPreview : MonoBehaviour
    {
        const int PreviewLayer = 31;
        public Camera previewCamera;
        public Shader characterShader;
        TreRuntimeAssets assets;
        readonly List<UObj> owned = new List<UObj>();
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

        T Own<T>(T item)
            where T : UObj
        {
            owned.Add(item);
            return item;
        }

        public void Initialize(TreRuntimeAssets source)
        {
            assets = source;
            previewCamera.enabled = true;
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
                var mesh = Own(new Mesh { indexFormat = IndexFormat.UInt32 });
                mesh.vertices = part.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
                mesh.normals = part.Normals.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
                mesh.uv = part.Uvs.Select(v => new Vector2(v.X, 1 - v.Y)).ToArray();
                mesh.triangles = part.Triangles;
                mesh.RecalculateBounds();
                g.GetComponent<MeshFilter>().sharedMesh = mesh;
                var shader = characterShader;
                if (shader == null)
                    throw new InvalidOperationException("SWG character shader is missing");
                var material = Own(new Material(shader));
                material.SetTexture("_BaseMap", assets.Texture(part.Diffuse));
                material.SetFloat("_Cutoff", sources != null && sources[partIndex].ObjectTemplate.StartsWith("object/tangible/hair/", StringComparison.Ordinal) ? .35f : 0);
                material.SetColor("_Tint", part.Tint == null ? Color.white : new Color32(part.Tint[0], part.Tint[1], part.Tint[2], 255));
                g.GetComponent<MeshRenderer>().sharedMaterial = material;
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
            foreach (var item in owned)
                if (item != null)
                {
                    if (Application.isPlaying)
                        Destroy(item);
                    else
                        DestroyImmediate(item);
                }
        }
    }
}
