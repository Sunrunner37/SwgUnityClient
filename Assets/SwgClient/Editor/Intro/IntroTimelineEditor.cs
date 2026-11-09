using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Intro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;

namespace Swg.Client.Editor.Intro
{
    public static class IntroTimelineEditor
    {
        [MenuItem("SWG/Edit Intro Timeline")]
        public static void Open()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("Stop Play mode before editing the intro Timeline.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(SwgProjectPaths.Scenes + "/" + ScreenId.Intro + ".unity");
            var screen = Object.FindAnyObjectByType<IntroScreen>();
            Selection.activeGameObject = screen.gameObject;
            var window = TimelineEditor.GetOrCreateWindow();
            window.SetTimeline(screen.sequence);
            window.Focus();
        }
    }
}
