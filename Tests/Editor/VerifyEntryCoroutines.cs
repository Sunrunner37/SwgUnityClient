using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using System;
using System.Threading.Tasks;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.CharacterList;
using Swg.Client.Runtime.UI.Screens.Loading;
using Swg.Client.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyEntryCoroutines
{
    public static async Task<object> Run()
    {
        var list = UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>();
        if (list == null || !list.IsReady)
            throw new Exception("Log in first.");
        list.next.onClick.Invoke();
        bool backClicked = false;
        bool hiddenCamera = false;
        bool sawReadyLoadingContent = false;
        var deadline = DateTime.UtcNow.AddSeconds(150);
        while (DateTime.UtcNow < deadline)
        {
            var loading = UnityEngine.Object.FindAnyObjectByType<LoadingScreen>();
            var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
            if (loading != null && (loading.content.alpha == 1) && !loading.entry.PlayerReady)
                sawReadyLoadingContent = true;
            if (loading != null && !backClicked)
            {
                if (loading.cancel.onClick.GetPersistentEventCount() != 0)
                    throw new Exception("Back has a persistent action.");
                loading.cancel.onClick.Invoke();
                backClicked = true;
            }

            if (loading != null && loading.entry.Failure != null)
                throw new Exception(loading.entry.Failure.Message);
            if (world != null && !world.IsReady)
            {
                if (world.orbit.worldCamera.enabled)
                    throw new Exception("Unfinished world was revealed.");
                hiddenCamera = true;
            }

            if (backClicked && SceneManager.GetActiveScene().name == "Login")
                throw new Exception("Back still cancelled entry.");
            if (world != null && world.IsReady && loading == null && SceneManager.GetActiveScene().name == "World")
            {
                if (!backClicked || !hiddenCamera || !sawReadyLoadingContent || !GameServices.Resolve<GameReplication>().CaptureSceneStatus().SceneReadyAcknowledged)
                    throw new Exception("Incomplete entry handoff.");
                await Task.Delay(1000);
                if (GameServices.Resolve<GameReplication>() == null)
                    throw new Exception("Loading teardown disconnected the live world.");
                return new
                {
                    loadingContentShownBeforeWorldReady = sawReadyLoadingContent,
                    backHasNoAction = true,
                    cameraHiddenUntilReady = true,
                    scene = "World",
                    scenery = world.StaticObjectCount,
                    entities = world.EntityCount,
                    sessionSurvivedHandoff = true
                };
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Coroutine entry timed out.");
    }
}
