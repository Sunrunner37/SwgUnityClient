using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Login;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyBindings
{
    public static async Task<object> Run()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var login = UnityEngine.Object.FindAnyObjectByType<LoginScreen>();
        if (login == null || !login.IsReady)
            throw new Exception("Open Login first.");
        var authored = login.GetComponentsInChildren<TreTextureBinding>(true).Where(binding => binding.isActiveAndEnabled && binding.asset != null).ToArray();
        while (authored.Any(binding => !binding.IsBound))
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Authored bindings did not initialize themselves.");
            await Task.Delay(25);
        }

        var root = new GameObject("Independent texture binding", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var image = root.AddComponent<RawImage>();
            var binding = root.AddComponent<TreTextureBinding>();
            binding.asset = authored[0].asset;
            root.SetActive(true);
            binding.enabled = false;
            if (!image.enabled)
                throw new Exception("Stopping a pending native binding did not restore visibility.");
            binding.enabled = true;
            while (!binding.IsBound)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Standalone binding did not initialize.");
                await Task.Delay(25);
            }

            if (root.GetComponentInParent<ClientScreen>() != null || image.texture == null || !image.enabled)
                throw new Exception("Binding required a screen or failed to restore visibility.");
            binding.enabled = false;
            binding.enabled = true;
            while (!binding.IsBound)
                await Task.Delay(25);
            if (image.texture == null || !image.enabled)
                throw new Exception("Re-enabled binding failed.");
            return new
            {
                authoredBindings = authored.Length,
                standaloneBinding = true,
                reenableWorks = true
            };
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
        }
    }
}
