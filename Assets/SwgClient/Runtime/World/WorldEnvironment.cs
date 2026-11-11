using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Swg.Client.Runtime.World
{
    /// <summary>Applies native environment lighting while the world scene is active.</summary>
    public sealed class WorldEnvironment : MonoBehaviour
    {
        static readonly int EnvironmentLightingId = Shader.PropertyToID("_SwgEnvironmentLighting");
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        [SerializeField] Light sun;
        [SerializeField] Camera cameraView;
        [Tooltip("Native lighting-ramp phase: 0.25 is midday; 0.75 is midnight.")]
        [Range(0, 1)] public float timeOfDay = .25f;
        WorldState world;
        Material originalSky;
        Material sky;
        float nextSample;
        TerrainEnvironmentData environment;
        TerrainEnvironmentData appliedEnvironment;
        int appliedColumn = -1;
        float appliedFarClip;
        public string CurrentEnvironment => environment?.Name;

        public void Initialize(WorldState world)
        {
            this.world = world;
            sun.enabled = false;
        }

        void OnEnable()
        {
            if (world == null) return;
            sun.enabled = true;
            RenderSettings.sun = sun;
            originalSky = RenderSettings.skybox;
            if (originalSky != null)
            {
                sky = new Material(originalSky);
                RenderSettings.skybox = sky;
            }
            Shader.SetGlobalFloat(EnvironmentLightingId, 1);
            nextSample = 0;
            appliedEnvironment = null;
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (world?.Terrain == null) return;
            if (Time.unscaledTime >= nextSample)
            {
                var position = world.SceneObjects.GetServerWorldPose(world.PlayerSwgId).position;
                environment = world.Terrain.Environments.Get(world.Terrain.Sample(position.x, position.z).EnvironmentFamily);
                nextSample = Time.unscaledTime + .25f;
            }
            if (environment == null) return;
            int column = environment.GetColumn(timeOfDay);
            float farClip = cameraView.farClipPlane;
            if (environment == appliedEnvironment && column == appliedColumn && farClip == appliedFarClip) return;
            Apply(environment.SampleColumn(column), farClip);
            appliedEnvironment = environment;
            appliedColumn = column;
            appliedFarClip = farClip;
        }

        void Apply(EnvironmentLighting lighting, float farClip)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = lighting.AmbientSky;
            RenderSettings.ambientEquatorColor = lighting.AmbientEquator;
            RenderSettings.ambientGroundColor = lighting.AmbientGround;
            sun.color = lighting.Sun;
            sun.intensity = lighting.SunIntensity;
            sun.shadows = environment.ShadowsEnabled ? LightShadows.Soft : LightShadows.None;
            RenderSettings.fog = environment.FogEnabled;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = lighting.Fog;
            // Native GroundEnvironment scales fog between its 512m and 2048m view distances.
            RenderSettings.fogDensity = Mathf.Lerp(environment.MinimumFogDensity, environment.MaximumFogDensity, Mathf.InverseLerp(512, 2048, farClip));
            cameraView.backgroundColor = lighting.Clear;
            if (sky != null && sky.HasProperty(SkyTintId)) sky.SetColor(SkyTintId, lighting.Clear);
        }

        void OnDisable()
        {
            if (world == null) return;
            Shader.SetGlobalFloat(EnvironmentLightingId, 0);
            sun.enabled = false;
            if (sky != null)
            {
                if (SceneManager.GetActiveScene() == gameObject.scene && RenderSettings.skybox == sky)
                    RenderSettings.skybox = originalSky;
                Destroy(sky);
                sky = null;
            }
            appliedEnvironment = null;
        }
    }
}
