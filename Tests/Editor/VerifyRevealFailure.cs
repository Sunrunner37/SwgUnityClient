using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Login;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyRevealFailure
{
    public static async Task<object> Run()
    {
        var broken = ScriptableObject.CreateInstance<TreTexture>();
        broken.swgPath = "texture/missing_reveal_test.dds";
        void Configure(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Login")
                return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var binding = root.GetComponentsInChildren<TreTextureBinding>(true).FirstOrDefault(item => item.isActiveAndEnabled && item.asset != null);
                if (binding != null)
                {
                    binding.asset = broken;
                    break;
                }
            }
        }

        string failure = null;
        void Log(string text, string stack, LogType type)
        {
            if (type == LogType.Exception && text.Contains("missing_reveal_test"))
                failure = text;
        }

        Application.logMessageReceived += Log;
        SceneManager.sceneLoaded += Configure;
        try
        {
            ScreenChanger.Open(ScreenId.Login);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            LoginScreen screen;
            do
            {
                await Task.Delay(25);
                screen = UnityEngine.Object.FindAnyObjectByType<LoginScreen>();
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Binding failure was not logged.");
            }
            while (screen == null || failure == null);
            if (screen.IsReady || (screen.content.alpha == 1) || screen.content.GetComponent<CanvasGroup>().alpha != 0)
                throw new Exception("Unfinished screen content was revealed.");
            return new
            {
                contentStayedHidden = true,
                errorLogged = true
            };
        }
        finally
        {
            Application.logMessageReceived -= Log;
            SceneManager.sceneLoaded -= Configure;
            ScreenChanger.Open(ScreenId.Login);
            UnityEngine.Object.Destroy(broken);
        }
    }
}
