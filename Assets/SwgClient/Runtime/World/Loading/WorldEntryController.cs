using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Networking.Chat;
using Swg.Client.Core.World;
using Autofac;
using Swg.Client.Core;
using System;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Runtime.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Swg.Client.Runtime.World.Loading
{
    /// <summary>Unity coroutine sequence for initial world entry and scene handoff.</summary>
    public sealed class WorldEntryController : MonoBehaviour
    {
        public WorldStreamingSettings settings;
        [Min(.01f)]
        public float sceneTimeout = 45;
        [Min(.01f)]
        public float worldBuildTimeout = 120;
        [Min(.01f)]
        public float sceneReadyTimeout = 20;
        public string Phase { get; private set; } = "Loading...";
        public float Progress { get; private set; }
        public bool PlayerReady { get; private set; }
        public string Destination { get; private set; }
        public Exception Failure { get; private set; }

        readonly CancellationTokenSource cancel = new CancellationTokenSource();
        ILifetimeScope session;
        GameReplication replication;
        WorldLoadingData prepared;
        WorldController world;
        Scene worldScene;
        AsyncOperation sceneLoad;
        Coroutine preparation;
        Exception sceneFailure;
        bool stopped;
        bool quitting;
        public Coroutine Begin()
        {
            if (preparation != null)
                return preparation;
            preparation = StartCoroutine(PrepareWorld());
            return preparation;
        }

        IEnumerator PrepareWorld()
        {
            try
            {
                session = GameServices.GameScope;
                if (session == null)
                    throw new InvalidOperationException("Select a character before entering the world.");
                replication = session.Resolve<GameReplication>();
                settings.Validate();
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            var steps = new IEnumerator[]
            {
                GetPlayerDestination(),
                AwaitPlayerBaselines(),
                LoadWorldAssets(),
                LoadUnityWorldScene(),
                BuildWorldGeometry(),
                AwaitServerFinalAcknowledgement()
            };
            var total = Stopwatch.StartNew();
            foreach (var step in steps)
            {
                var timer = Stopwatch.StartNew();
                yield return step;
                Debug.Log($"World entry � {Phase}: {timer.Elapsed.TotalSeconds:F2}s");
                if (stopped)
                    yield break;
            }

            try
            {
                session.Resolve<ClientChat>().RequestRooms();
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            Debug.Log($"World entry complete: {total.Elapsed.TotalSeconds:F2}s");
            PlayerReady = true;
            Progress = 1;
        }

        IEnumerator GetPlayerDestination()
        {
            Phase = "Waiting for player destination";
            yield return WaitFor(() => replication.CaptureSceneStatus().StartScene.HasValue, sceneTimeout, "The server did not send the destination scene.");
            if (stopped)
                yield break;
            Destination = Path.GetFileNameWithoutExtension(replication.CaptureSceneStatus().StartScene.Value.TerrainPath);
        }

        IEnumerator AwaitPlayerBaselines()
        {
            Phase = "Receiving player and world objects";
            yield return WaitFor(() => replication.CaptureSceneStatus().PlayerBaselinesComplete, sceneTimeout, "The server did not finish sending the player scene.");
        }

        IEnumerator LoadWorldAssets()
        {
            Phase = "Loading world assets";
            var task = LoadAssetsAsync();
            _ = task.ContinueWith(completed =>
            {
                _ = completed.Exception;
            }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted | System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                Fail(task.Exception.GetBaseException());
                yield break;
            }

            if (task.IsCanceled)
                yield break;
            prepared = task.Result;
            Progress = .6f;
        }

        // One-time bootstrap; region and entity decoding stay shared with ongoing streaming.
        async Task<WorldLoadingData> LoadAssetsAsync()
        {
            var token = cancel.Token;
            var progress = new Progress<WorldPreparationProgress>(Report);
            var loadingSession = session;
            token.ThrowIfCancellationRequested();
            var status = replication.CaptureSceneStatus();
            if (!status.StartScene.HasValue || !status.PlayerBaselinesComplete)
                throw new InvalidOperationException("Wait for the player destination and baselines before loading world assets.");
            var initial = replication.CaptureSnapshot();
            var state = loadingSession.Resolve<WorldState>();
            var content = loadingSession.Resolve<WorldContentLoader>();
            await content.OpenAsync(initial, token);
            replication.SetTemplates(state.Templates);
            var data = new WorldLoadingData();
            data.InitialRegion = await content.LoadRegionAsync(state.RenderOrigin, settings, new HashSet<Vector2Int>(), new HashSet<SwgId>(), progress, token);
            var appearances = AppearanceRequest.Capture(state, initial);
            foreach (var pair in await content.LoadAppearancesAsync(initial, appearances, token))
                data.InitialAppearances.Add(pair.Key, pair.Value);
            if (!data.InitialAppearances.TryGetValue(initial.PlayerSwgId, out var player) || player.Parts.Count == 0)
                throw new InvalidDataException("The selected player's native appearance could not be loaded.");
            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(loadingSession, GameServices.GameScope))
                throw new InvalidOperationException("The world session changed.");
            return data;
        }

        IEnumerator LoadUnityWorldScene()
        {
            Phase = "Loading Unity world scene";
            try
            {
                string sceneName = ScreenId.World.ToString();
                if (SceneManager.GetSceneByName(sceneName).IsValid())
                    throw new InvalidOperationException("A world scene is already loaded or loading.");
                SceneManager.sceneLoaded += WorldSceneLoaded;
                sceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (sceneLoad == null)
                    throw new InvalidOperationException("Unity could not start loading the world scene.");
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            yield return sceneLoad;
            if (sceneFailure != null)
                Fail(sceneFailure);
        }

        IEnumerator BuildWorldGeometry()
        {
            Phase = "Preparing world geometry";
            yield return WaitFor(() =>
            {
                Progress = .6f + .35f * world.Progress;
                return world.IsReady;
            }, worldBuildTimeout, "World preparation timed out.");
        }

        IEnumerator AwaitServerFinalAcknowledgement()
        {
            Phase = "Awaiting scene-ready acknowledgement";
            Progress = .98f;
            try
            {
                GameServices.Resolve<GameConnection>().Send(new CmdSceneReadyPacket());
            }
            catch (Exception error)
            {
                Fail(error);
                yield break;
            }

            yield return WaitFor(() => replication.CaptureSceneStatus().SceneReadyAcknowledged, sceneReadyTimeout, "The server did not confirm world entry.");
        }

        IEnumerator WaitFor(Func<bool> completed, float timeout, string message)
        {
            Exception failure = null;
            yield return new WaitUntil(() =>
            {
                try
                {
                    return completed();
                }
                catch (Exception error)
                {
                    failure = error;
                    return true;
                }
            }, TimeSpan.FromSeconds(timeout), () => failure = new TimeoutException(message));
            if (failure != null)
                Fail(failure);
        }

        void WorldSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != ScreenId.World.ToString())
                return;
            SceneManager.sceneLoaded -= WorldSceneLoaded;
            worldScene = scene;
            if (stopped)
            {
                UnloadWorld();
                return;
            }

            try
            {
                var controllers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldController>(true)).ToArray();
                if (controllers.Length != 1 || !controllers[0].isActiveAndEnabled)
                    throw new InvalidOperationException("The world scene must contain one active WorldController.");
                world = controllers[0];
                world.Initialize(session, prepared, settings);
                prepared = null;
            }
            catch (Exception error)
            {
                sceneFailure = error;
                UnloadWorld();
            }
        }

        public void Activate(Scene presentationScene)
        {
            if (stopped || !PlayerReady || world == null || !world.IsReady || !worldScene.isLoaded)
                throw new InvalidOperationException("The prepared world is no longer available.");
            if (!SceneManager.SetActiveScene(worldScene))
                throw new InvalidOperationException("Unity could not activate the world scene.");
            foreach (var root in presentationScene.GetRootGameObjects())
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                    listener.enabled = false;
            world.Reveal();
            // The world now owns its lifetime; loading-scene teardown must leave it running.
            worldScene = default;
            session = null;
            SceneManager.UnloadSceneAsync(presentationScene);
        }

        void Report(WorldPreparationProgress value)
        {
            if (stopped || this == null)
                return;
            Phase = value.Phase;
            Progress = Mathf.Max(Progress, Mathf.Clamp01(value.Fraction) * .6f);
        }

        void Fail(Exception error)
        {
            Failure = error;
            Abort();
        }

        public void Abort()
        {
            if (stopped)
                return;
            stopped = true;
            cancel.Cancel();
            prepared = null;
            if (session != null && ReferenceEquals(GameServices.GameScope, session))
                GameServices.Disconnect();
            UnloadWorld();
            if (sceneLoad == null || sceneLoad.isDone)
                SceneManager.sceneLoaded -= WorldSceneLoaded;
            StopAllCoroutines();
        }

        void UnloadWorld()
        {
            if (quitting || !Application.isPlaying || !worldScene.IsValid() || !worldScene.isLoaded)
                return;
            foreach (var root in worldScene.GetRootGameObjects())
                root.SetActive(false);
            SceneManager.UnloadSceneAsync(worldScene);
            worldScene = default;
        }

        void OnApplicationQuit() => quitting = true;
        void OnDestroy()
        {
            Abort();
            cancel.Cancel();
            cancel.Dispose();
            if (quitting || !Application.isPlaying || sceneLoad == null || sceneLoad.isDone)
                SceneManager.sceneLoaded -= WorldSceneLoaded;
        }
    }
}
