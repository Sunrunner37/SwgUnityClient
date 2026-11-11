using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using System;
using System.Threading.Tasks;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.CharacterList;
using Swg.Client.Runtime.UI.Screens.Loading;
using Swg.Client.Runtime.UI.Screens.Login;
using Swg.Client.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyLoading
{
    public static async Task<object> Run(string mode, string variant)
    {
        void Configure(Scene scene, LoadSceneMode loadMode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (mode == "worldfailure" && scene.name == "World")
                {
                    var world = root.GetComponentInChildren<WorldController>();
                    if (world != null)
                        world.worldShader = null;
                }

                if (scene.name != variant)
                    continue;
                var loading = root.GetComponentInChildren<LoadingScreen>();
                if (loading == null)
                    continue;
                if (mode == "failure")
                {
                    loading.view.contentTable = "datatables/loading/missing_test_table.iff";
                    loading.view.fallbackTable = loading.view.contentTable;
                }

                if (mode == "timeout")
                {
                    loading.entry.sceneTimeout = .05f;
                    // Keep the selected client state empty; live packets continue targeting the original state.
                    var session = ClientRuntime.Ensure().Session;
                    session.GetType().GetProperty("World").SetValue(session, new Swg.Client.Core.Networking.Game.GameReplication());
                }
            }
        }

        SceneManager.sceneLoaded += Configure;
        try
        {
            var list = UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>();
            if (list == null || !list.IsReady)
                throw new Exception("Character list not ready.");
            if (variant == "LoadingThematic")
                list.next.onClick.Invoke();
            else
            {
                var session = ClientRuntime.Ensure().Session;
                session.SelectCharacter(session.Admission.Characters[0].SwgId);
                ScreenChanger.Open(ScreenId.LoadingHorizontal);
            }

            var deadline = DateTime.UtcNow.AddSeconds(120);
            LoadingScreen screen = null;
            bool reached = false;
            bool disconnected = false;
            while (DateTime.UtcNow < deadline)
            {
                screen = UnityEngine.Object.FindAnyObjectByType<LoadingScreen>();
                var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
                if (mode == "disconnect" && !disconnected && screen?.entry.Destination != null)
                {
                    ClientRuntime.Ensure().Session.Disconnect("Expected test disconnect");
                    disconnected = true;
                }

                if (mode == "teardown" && screen != null && world != null && !world.IsReady)
                {
                    reached = true;
                    break;
                }

                if (mode != "teardown" && screen != null && screen.entry.Failure != null)
                {
                    reached = true;
                    break;
                }

                await Task.Delay(20);
            }

            if (!reached || screen == null)
                throw new TimeoutException("Expected entry stage was not reached.");
            if (mode != "teardown" && (screen.IsReady || GameServices.Resolve<GameReplication>() != null))
                throw new Exception("Failed entry became ready or retained its session.");
            if (mode == "disconnect" && (!disconnected || !screen.entry.Failure.Message.Contains("Expected test disconnect")))
                throw new Exception("Central connection-loss handling did not preserve the failure reason.");
            screen.cancel.onClick.Invoke();
            await Task.Delay(50);
            if (UnityEngine.Object.FindAnyObjectByType<LoadingScreen>() == null)
                throw new Exception("Back still has an action.");
            // Explicit test teardown, not a player-facing cancel action.
            ScreenChanger.Open(ScreenId.Login);
            while (DateTime.UtcNow < deadline)
            {
                if (UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady == true)
                {
                    await Task.Delay(500);
                    if (GameServices.Resolve<GameReplication>() != null || UnityEngine.Object.FindAnyObjectByType<WorldController>() != null || SceneManager.GetSceneByName("World").IsValid())
                        throw new Exception("Partial world survived scene teardown.");
                    return new
                    {
                        mode,
                        variant,
                        noLeakedWorld = true,
                        backHasNoAction = true
                    };
                }

                await Task.Delay(20);
            }

            throw new TimeoutException("Scene teardown did not finish.");
        }
        finally
        {
            SceneManager.sceneLoaded -= Configure;
        }
    }
}
