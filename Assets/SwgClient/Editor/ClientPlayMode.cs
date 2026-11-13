using Swg.Client.Runtime.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Swg.Client.Editor
{
    [InitializeOnLoad]
    public static class ClientPlayMode
    {
        static ClientPlayMode()
        {
            EditorApplication.delayCall += SetDefaultStartScene;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.delayCall += SetDefaultStartScene;
                }
            };
        }

        static void SetDefaultStartScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SwgProjectPaths.Scenes + "/" + ScreenId.Intro + ".unity");
        }

        public static void PlayClient()
        {
            if (EditorApplication.isPlaying)
                return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SwgProjectPaths.Scenes + "/" + ScreenId.Intro + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("SWG/Play UI Test Scene")]
        public static void PlayUiTesting()
        {
            if (EditorApplication.isPlaying)
                return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SwgProjectPaths.UiTestingScene);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("SWG/Preview Current Screen")]
        public static void PreviewCurrentScreen()
        {
            if (EditorApplication.isPlaying)
                return;
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }
    }
}
