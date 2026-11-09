using Swg.Client.Runtime.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Swg.Client.Runtime;

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
                if (state == PlayModeStateChange.ExitingEditMode && !SessionState.GetBool(ClientNavigation.PreviewSessionKey, false))
                    SetDefaultStartScene();
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(ClientNavigation.PreviewSessionKey, false);
                    SetDefaultStartScene();
                }
            };
        }

        static void SetDefaultStartScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlaying || SessionState.GetBool(ClientNavigation.PreviewSessionKey, false))
                return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SwgProjectPaths.Scenes + "/" + ScreenId.Intro + ".unity");
        }

        public static void PlayClient()
        {
            if (EditorApplication.isPlaying)
                return;
            SessionState.SetBool(ClientNavigation.PreviewSessionKey, false);
            SetDefaultStartScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("SWG/Preview Current Screen")]
        public static void PreviewCurrentScreen()
        {
            if (EditorApplication.isPlaying)
                return;
            SessionState.SetBool(ClientNavigation.PreviewSessionKey, true);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }
    }
}
