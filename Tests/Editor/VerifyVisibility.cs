using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Intro;
using Swg.Client.Runtime.UI.Screens.Login;
using Swg.Client.Runtime.UI.Screens.CharacterCreation;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyVisibility
{
    public static async Task<object> Run()
    {
        int hiddenAtLoad = 0;
        Exception failure = null;
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var content = root.GetComponentsInChildren<CanvasGroup>(true).FirstOrDefault(item => item.name == "Content");
                if (content == null)
                    continue;
                var group = content.GetComponent<CanvasGroup>();
                if (group.alpha != 0 || group.interactable || group.blocksRaycasts)
                    failure = new Exception("Screen was visible during scene startup: " + scene.name);
                hiddenAtLoad++;
            }
        }

        SceneManager.sceneLoaded += Loaded;
        var setting = GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>().GetType().GetProperty("SkipIntro");
        object original = setting.GetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>());
        try
        {
            ScreenChanger.Open(ScreenId.Login);
            await Ready<LoginScreen>();
            ScreenChanger.Open(ScreenId.CharacterCreation);
            await Ready<CreationScreen>();
            var creation = UnityEngine.Object.FindAnyObjectByType<CreationScreen>();
            if (!creation.preview.previewCamera.enabled || creation.preview.GetComponentsInChildren<MeshRenderer>().Length == 0)
                throw new Exception("Initial character preview was not ready at reveal.");
            setting.SetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>(), false);
            ScreenChanger.Open(ScreenId.Intro);
            await Ready<IntroScreen>();
            UnityEngine.Object.FindAnyObjectByType<IntroScreen>().Skip();
            await Ready<LoginScreen>();
            if (failure != null)
                throw failure;
            return new
            {
                hiddenAtLoad,
                revealAfterTextures = true,
                characterPreviewReadyAtReveal = true
            };
        }
        finally
        {
            setting.SetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>(), original);
            SceneManager.sceneLoaded -= Loaded;
        }
    }

    static async Task Ready<T>()
        where T : ClientScreen
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        T screen;
        do
        {
            await Task.Delay(25);
            screen = UnityEngine.Object.FindAnyObjectByType<T>();
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Screen did not reveal: " + typeof(T).Name);
        }
        while (screen == null || !screen.IsReady);
        var content = screen.GetComponentsInChildren<CanvasGroup>(true).First(item => item.name == "Content");
        if (content.GetComponent<CanvasGroup>().alpha != 1 || content.GetComponentsInChildren<TreTextureBinding>().Any(binding => binding.isActiveAndEnabled && binding.asset != null && !binding.IsBound))
            throw new Exception("Ready screen contains unfinished visuals.");
    }
}
