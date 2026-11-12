using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI.Screens;

namespace Swg.Client.Editor
{
    [InitializeOnLoad]
    public static class FrontendSceneView
    {
        // Unity restores the Scene view camera after sceneOpened. Frame on its first
        // GUI draw instead, so the restored camera cannot overwrite our framing.
        static readonly Dictionary<SceneView, Scene> framedScenes = new Dictionary<SceneView, Scene>();
        static FrontendSceneView()
        {
            EditorSceneManager.sceneOpened += (_, __) => framedScenes.Clear();
            PrefabStage.prefabStageOpened += _ => framedScenes.Clear();
            PrefabStage.prefabStageClosing += _ => framedScenes.Clear();
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.playModeStateChanged += _ => framedScenes.Clear();
        }

        static RectTransform Target(out Scene scene)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                scene = stage.scene;
                var root = stage.prefabContentsRoot;
                if (root.GetComponentInChildren<TreTextureBinding>(true) == null)
                    return null;
                var panel = root.GetComponent<RectTransform>();
                if (panel != null)
                    return panel;
                var prefabCanvas = root.GetComponentInChildren<Canvas>(true);
                return prefabCanvas == null ? null : (RectTransform)prefabCanvas.transform;
            }

            scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
                return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var app = root.GetComponentInChildren<ClientScreen>(true);
                if (app == null && scene.path != SwgProjectPaths.UiTestingScene)
                    continue;
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    return (RectTransform)canvas.transform;
            }

            return null;
        }

        static void OnSceneGui(SceneView view)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var rect = Target(out var scene);
            if (rect == null)
                return;
            if (framedScenes.TryGetValue(view, out var framed) && framed.Equals(scene))
                return;
            // Frame once per opened scene/view. Manual panning/zooming is retained.
            if (Frame(view, rect))
                framedScenes[view] = scene;
        }

        [MenuItem("SWG/Frame Frontend in Scene View")]
        public static void FrameFrontend()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var rect = Target(out var scene);
            var view = SceneView.lastActiveSceneView;
            if (rect != null && view != null)
            {
                if (Frame(view, rect))
                    framedScenes[view] = scene;
            }
        }

        static bool Frame(SceneView view, RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bounds = new Bounds(corners[0], Vector3.zero);
            foreach (var corner in corners)
                bounds.Encapsulate(corner);
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
                return false;
            // Look straight at the Canvas, using its rotation rather than a cached
            // perspective view from another scene or the previous overlay layout.
            if (Quaternion.Angle(rect.rotation, Quaternion.identity) < .01f)
                view.in2DMode = true;
            else
            {
                view.in2DMode = false;
                view.rotation = rect.rotation;
            }

            view.orthographic = true;
            view.pivot = bounds.center;
            view.size = Mathf.Max(bounds.size.y * .65f, bounds.size.x / view.camera.aspect * .65f);
            view.Repaint();
            return true;
        }
    }
}
