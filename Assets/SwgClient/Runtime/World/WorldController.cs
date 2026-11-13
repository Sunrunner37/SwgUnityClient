using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Audio;
using Autofac;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Collections;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    public sealed class WorldController : MonoBehaviour
    {
        public GameObject hud;
        public Shader worldShader;
        public Shader characterShader;
        public Shader particleShader;
        public WorldOrbitCamera orbit;
        public WorldEnvironment environment;
        public WorldWaypointRenderer waypoints;
        public bool IsReady { get; private set; }
        public float Progress => IsReady ? 1 : Streaming?.Progress ?? 0;
        public WorldStreamer Streaming { get; private set; }
        public WorldContentLoader Content { get; private set; }
        public SceneObjectRegistry SceneObjects => this.state?.SceneObjects;

        WorldState state;
        WorldLoadingData loadedData;
        ILifetimeScope session;
        WorldStreamingSettings settings;
        WorldAppearanceLibrary appearanceLibrary;
        WorldSceneObjectRenderer objectRenderer;
        WorldFloraRenderer flora;
        bool revealed;
        public void Initialize(ILifetimeScope session, WorldLoadingData sceneData, WorldStreamingSettings streamingSettings)
        {
            if (this.state != null)
                throw new InvalidOperationException("WorldController was already initialized.");
            loadedData = sceneData ?? throw new ArgumentNullException(nameof(sceneData));
            this.session = session;
            this.state = session.Resolve<WorldState>();
            Content = session.Resolve<WorldContentLoader>();
            settings = streamingSettings ?? throw new ArgumentNullException(nameof(streamingSettings));
        }

        IEnumerator Start()
        {
            try
            {
                if (hud != null)
                    hud.SetActive(false);
                orbit.worldCamera.enabled = false;
                orbit.enabled = false;
                objectRenderer = new GameObject("World Objects").AddComponent<WorldSceneObjectRenderer>();
                objectRenderer.transform.SetParent(transform, false);
                objectRenderer.enabled = false;
                flora = new GameObject("Procedural flora").AddComponent<WorldFloraRenderer>();
                flora.transform.SetParent(transform, false);
                flora.Initialize(this.state, worldShader, particleShader, settings, orbit.worldCamera);
                flora.enabled = false;
                orbit.worldCamera.farClipPlane = settings.terrainViewDistance;
                environment.Initialize(this.state);
                session.Resolve<WorldRenderingContext>().Initialize(worldShader, characterShader, particleShader, settings, objectRenderer, this, flora);
                session.Resolve<WorldRenderingContext>().WorldCamera = orbit.worldCamera;
                appearanceLibrary = session.Resolve<WorldAppearanceLibrary>();
                objectRenderer.Initialize(this.state, Content, loadedData, session.Resolve<GameReplication>(), appearanceLibrary, settings.uploadsPerFrame, flora);
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
            environment.enabled = true;
            flora.enabled = true;
            waypoints.enabled = true;
            revealed = true;
            objectRenderer.enabled = true;
            orbit.enabled = true;
            orbit.worldCamera.enabled = true;
            orbit.worldCamera.GetComponent<WorldTargeting>().enabled = true;
            ManagedSceneObject.CurrentPlayer.GetComponent<PlayerMovement>().enabled = true;
            foreach (var listener in GetComponentsInChildren<AudioListener>(true))
                listener.enabled = true;
            if (hud != null)
                hud.SetActive(true);
            GameServices.Resolve<ClientMusic>().Stop();
        }

        void Update()
        {
            if (!revealed || !IsReady)
                return;
            if (SceneObjects.TryGet<SpatialSceneObject>(this.state.PlayerSwgId, out var player))
                Streaming.Update(SceneObjects.GetServerWorldPose(player.SwgId).position);
        }

        void OnDestroy()
        {
            GameServices.EndGame(session);
        }
    }
}
