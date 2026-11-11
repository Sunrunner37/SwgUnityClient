using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>A weighted appearance or radial shader from the terrain's FGRP/RGRP families.</summary>
    public sealed class TerrainFloraItem
    {
        public string Path;
        public float Weight;
        public bool Radial;
        public bool Floats;
        public bool AlignToTerrain;
        public bool Crossed;
        public float MinimumWidth = 1;
        public float MaximumWidth = 1;
        public float MinimumHeight = 1;
        public float MaximumHeight = 1;
        public bool MaintainAspectRatio;
        // Native radial widths are half-widths, measured from the quad center.
        public Vector3 RadialSize(float widthChoice, float heightChoice)
        {
            float width = Mathf.Lerp(MinimumWidth, MaximumWidth, widthChoice);
            float heightAmount = MaintainAspectRatio ? (MaximumWidth != MinimumWidth ? widthChoice : 0) : heightChoice;
            float height = Mathf.Lerp(MinimumHeight, MaximumHeight, heightAmount);
            return new Vector3(width * 2, height, width * 2);
        }

        public float Distance;
        public float MinimumScale = 1;
        public float MaximumScale = 1;
        public float SwayDisplacement;
        public float SwayPeriod;
    }
}
