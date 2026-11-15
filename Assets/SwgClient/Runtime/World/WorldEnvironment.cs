using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Assets.Tre;
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

        [SerializeField]
        Light sun;
        [SerializeField]
        Camera cameraView;
        [SerializeField]
        Shader skyShader;
        [Tooltip("Native lighting-ramp phase: 0.25 is midday; 0.75 is midnight.")]
        [Range(0, 1)]
        public float timeOfDay = .25f;
        WorldState state;
        CurrentPlayer currentPlayer;
        TreRuntimeAssets assets;
        Material originalSky;
        Material sky;
        float nextSample;
        TerrainEnvironmentData environment;
        TerrainEnvironmentData appliedEnvironment;
        int appliedColumn = -1;
        float appliedFarClip;
        public string CurrentEnvironment => environment?.Name;

        public void Initialize(WorldState state)
        {
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            this.state = state;
            assets = GameServices.Resolve<TreRuntimeAssets>();
            sun.enabled = false;
        }

        void OnEnable()
        {
            if (this.state == null)
                return;
            sun.enabled = true;
            RenderSettings.sun = sun;
            originalSky = RenderSettings.skybox;
            sky = new Material(skyShader);
            RenderSettings.skybox = sky;

            Shader.SetGlobalFloat(EnvironmentLightingId, 1);
            nextSample = 0;
            appliedEnvironment = null;
            Refresh();
        }

        void Update() => Refresh();
        void Refresh()
        {
            if (this.state?.Terrain == null)
                return;
            if (Time.unscaledTime >= nextSample)
            {
                var position = this.state.SceneObjects.GetServerWorldPose(currentPlayer.SwgId).position;
                environment = this.state.Terrain.Environments.Get(this.state.Terrain.Sample(position.x, position.z).EnvironmentFamily);
                nextSample = Time.unscaledTime + .25f;
            }

            if (environment == null)
                return;
            int column = environment.GetColumn(timeOfDay);
            float farClip = cameraView.farClipPlane;
            if (environment == appliedEnvironment && column == appliedColumn && farClip == appliedFarClip)
                return;
            if (environment != appliedEnvironment)
            {
                sky.SetTexture("_Gradient", environment.SkyGradient == null ? null : assets.Texture(environment.SkyGradient, TextureWrapMode.Clamp));
                sky.SetFloat("_HasGradient", environment.SkyGradient == null ? 0 : 1);
                SetClouds("_Bottom", environment.BottomClouds);
                SetClouds("_Top", environment.TopClouds);
            }
            sky.SetFloat("_DayPhase", timeOfDay);
            Apply(environment.SampleColumn(column), farClip);
            appliedEnvironment = environment;
            appliedColumn = column;
            appliedFarClip = farClip;
        }

        void SetClouds(string prefix, TerrainEnvironmentData.CloudLayer clouds)
        {
            sky.SetFloat(prefix + "Enabled", clouds == null ? 0 : 1);
            if (clouds == null) return;
            sky.SetTexture(prefix + "Map", assets.Texture(clouds.Diffuse, TextureWrapMode.Repeat));
            sky.SetTexture(prefix + "Normal", clouds.Normal == null ? null : assets.Texture(clouds.Normal, TextureWrapMode.Repeat, linear: true));
            sky.SetVector(prefix + "Motion", new Vector4(clouds.Size, clouds.Speed, 0, 0));
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
            sky.SetColor("_Fallback", lighting.Clear);
            sky.SetColor("_CloudAmbient", lighting.AmbientEquator);
            sky.SetColor("_CloudSun", lighting.Sun * lighting.SunIntensity);
            sky.SetVector("_SunDirection", -sun.transform.forward);
        }

        void OnDisable()
        {
            if (this.state == null)
                return;
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
