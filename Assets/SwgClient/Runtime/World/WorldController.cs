using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Audio;
using Autofac;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Collections;
using Swg.Client.Runtime.UI.Screens;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    public sealed class WorldController : MonoBehaviour
    {
        ClientConnection connection;
        public Shader worldShader;
        public Shader characterShader;
        public WorldOrbitCamera orbit;
        public bool IsReady { get; private set; }
        public float Progress => IsReady ? 1 : Streaming?.Progress ?? 0;
        public WorldStreamer Streaming { get; private set; }
        public WorldContentLoader Content { get; private set; }
        public SceneObjectRegistry SceneObjects => worldState?.SceneObjects;

        WorldState worldState;
        WorldLoadingData loadedData;
        ILifetimeScope session;
        WorldStreamingSettings settings;
        WorldMeshLibrary meshes;
        WorldSceneObjectRenderer objectRenderer;
        bool revealed;

        public void Initialize(ILifetimeScope session, WorldLoadingData sceneData, WorldStreamingSettings streamingSettings)
        {
            if (worldState != null)
                throw new InvalidOperationException("WorldController was already initialized.");
            loadedData = sceneData ?? throw new ArgumentNullException(nameof(sceneData));
            this.session = session;
            worldState = session.Resolve<WorldState>();
            Content = session.Resolve<WorldContentLoader>();
            settings = streamingSettings ?? throw new ArgumentNullException(nameof(streamingSettings));
        }

        IEnumerator Start()
        {
            connection = GameServices.Resolve<ClientConnection>();
            try
            {
                orbit.worldCamera.enabled = false;
                orbit.enabled = false;
                objectRenderer = new GameObject("World Objects").AddComponent<WorldSceneObjectRenderer>();
                objectRenderer.transform.SetParent(transform, false);
                objectRenderer.enabled = false;
                session.Resolve<WorldRenderingSettings>().Initialize(worldShader, characterShader, settings, objectRenderer, this);
                meshes = session.Resolve<WorldMeshLibrary>();
                objectRenderer.Initialize(worldState, Content, loadedData, session.Resolve<GameReplication>(), meshes, settings.uploadsPerFrame);
                Streaming = session.Resolve<WorldStreamer>();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                yield break;
            }

            yield return Streaming.LoadInitial(loadedData.InitialRegion);
            loadedData.InitialRegion = null;

            loadedData = null;
            IsReady = true;
        }

        public void Reveal()
        {
            if (!IsReady)
                throw new InvalidOperationException("The world is not ready.");
            revealed = true;
            objectRenderer.enabled = true;
            orbit.enabled = true;
            orbit.worldCamera.enabled = true;
            orbit.target = ManagedSceneObject.CurrentPlayer.transform;
            foreach (var listener in GetComponentsInChildren<AudioListener>(true))
                listener.enabled = true;
            GameServices.Resolve<ClientMusic>().Stop();
        }

        void Update()
        {
            if (!revealed || !IsReady)
                return;

            if (connection.FailureReason != null)
            {
                ScreenChanger.Open(ScreenId.Login);
                return;
            }

            if (SceneObjects.TryGet<SpatialSceneObject>(worldState.PlayerSwgId, out var player))
                Streaming.Update(SceneObjects.GetServerWorldPose(player.SwgId).position);
        }

        void OnDestroy()
        {
            GameServices.EndGame(session);
        }
    }
}
