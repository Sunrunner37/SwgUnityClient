using System;
using Swg.Client.Runtime.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swg.Client.Runtime
{
    public static class ClientNavigation
    {
#if UNITY_EDITOR
        public const string PreviewSessionKey="Swg.Client.PreviewCurrentScreen";
#endif
        public static bool IsChanging
        {
            get; private set;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => IsChanging = false;
        public static void Open(ScreenId screen)
        {
            if (IsChanging)
                return;
            var scene = Enum.GetName(typeof(ScreenId), screen) ?? throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown screen.");
            IsChanging = true;
            try
            {
                SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single).completed += _ => IsChanging = false;
            }
            catch
            {
                IsChanging = false;
                throw;
            }
        }

        public static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
        }
    }
}
