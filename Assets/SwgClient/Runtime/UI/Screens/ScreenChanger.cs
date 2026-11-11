using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swg.Client.Runtime.UI.Screens
{
    public static class ScreenChanger
    {
        static AsyncOperation pendingLoad;

        public static void Open(ScreenId screen)
        {
            var sceneName = Enum.GetName(typeof(ScreenId), screen) ?? throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown screen.");
            if (pendingLoad != null && !pendingLoad.isDone)
            {
                Debug.LogWarning($"ScreenChanger: Ignoring request for {screen} because a screen load is already in progress.");
                return;
            }

            pendingLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
