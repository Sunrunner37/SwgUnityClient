using System.Collections.Generic;
using Swg.Client.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swg.Client.Editor
{
    /// <summary>Starts world inspection at the player instead of retaining the frontend's canvas framing.</summary>
    [InitializeOnLoad]
    public static class WorldSceneView
    {
        static readonly Dictionary<SceneView, Scene> framedScenes = new Dictionary<SceneView, Scene>();

        static WorldSceneView()
        {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.playModeStateChanged += _ => framedScenes.Clear();
        }

        static void OnSceneGui(SceneView view)
        {
            if (!EditorApplication.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            var scene = SceneManager.GetActiveScene();
            if (framedScenes.TryGetValue(view, out var framed) && framed == scene) return;
            var orbit = Object.FindAnyObjectByType<WorldOrbitCamera>();
            if (orbit == null || orbit.gameObject.scene != scene || orbit.target == null || orbit.worldCamera == null) return;
            Frame(view, orbit);
            framedScenes[view] = scene;
        }

        [MenuItem("SWG/Frame Player in Scene View")]
        public static void FramePlayer()
        {
            if (!EditorApplication.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            var view = SceneView.lastActiveSceneView;
            var orbit = Object.FindAnyObjectByType<WorldOrbitCamera>();
            if (view == null || orbit == null || orbit.target == null || orbit.worldCamera == null) return;
            Frame(view, orbit);
            framedScenes[view] = orbit.gameObject.scene;
        }

        static void Frame(SceneView view, WorldOrbitCamera orbit)
        {
            // Scene visibility affects only the editor; Canvas.enabled/SetActive would also hide the in-game HUD.
            foreach (var root in orbit.gameObject.scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                if (canvas.renderMode != RenderMode.WorldSpace)
                    SceneVisibilityManager.instance.Hide(canvas.gameObject, true);

            view.in2DMode = false;
            view.orthographic = false;
            view.size = 8;
            view.AlignViewToObject(orbit.worldCamera.transform);
            view.Repaint();
        }
    }
}
