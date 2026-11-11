using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    public readonly struct EnvironmentLighting
    {
        public readonly Color AmbientSky;
        public readonly Color AmbientEquator;
        public readonly Color AmbientGround;
        public readonly Color Sun;
        public readonly float SunIntensity;
        public readonly Color Clear;
        public readonly Color Fog;
        public EnvironmentLighting(Color ambientSky, Color ambientEquator, Color ambientGround, Color sun, float sunIntensity, Color clear, Color fog)
        {
            AmbientSky = ambientSky;
            AmbientEquator = ambientEquator;
            AmbientGround = ambientGround;
            Sun = sun;
            SunIntensity = sunIntensity;
            Clear = clear;
            Fog = fog;
        }
    }
}
