using System;
using System.Threading.Tasks;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Intro;
using Swg.Client.Runtime.UI.Screens.Login;
using Swg.Client.Runtime.UI.Screens.CharacterCreation;
using UnityEngine;

public static class VerifyScreens
{
    public static async Task<object> Run()
    {
        var property = GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>().GetType().GetProperty("SkipIntro");
        object original = property.GetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>());
        try
        {
            property.SetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>(), false);
            ScreenChanger.Open(ScreenId.Intro);
            await Wait(() => UnityEngine.Object.FindAnyObjectByType<IntroScreen>()?.IsReady == true);
            UnityEngine.Object.FindAnyObjectByType<IntroScreen>().Skip();
            await Wait(() => UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady == true);
            property.SetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>(), true);
            ScreenChanger.Open(ScreenId.Intro);
            await Wait(() => UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady == true);
            ScreenChanger.Open(ScreenId.CharacterCreation);
            await Wait(() => UnityEngine.Object.FindAnyObjectByType<CreationScreen>()?.IsReady == true);
            var creation = UnityEngine.Object.FindAnyObjectByType<CreationScreen>();
            if (string.IsNullOrEmpty(creation.speciesName.text))
                throw new Exception("Creation data was not populated.");
            creation.gender.onClick.Invoke();
            creation.speciesButtons[1].onClick.Invoke();
            creation.Back();
            await Wait(() => UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady == true);
            return new
            {
                intro = true,
                skipIntro = true,
                characterCreation = true
            };
        }
        finally
        {
            property.SetValue(GameServices.Resolve<Swg.Client.Core.Configuration.ClientSettings>(), original);
        }
    }

    static async Task Wait(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        do
        {
            await Task.Delay(25);
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Screen did not become ready.");
        }
        while (!ready());
    }
}
