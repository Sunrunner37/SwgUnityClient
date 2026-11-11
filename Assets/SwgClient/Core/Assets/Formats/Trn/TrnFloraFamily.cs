using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnFloraFamily
    {
        public int Id;
        public string Name;
        public Color32 EditorColor;
        public float Density;
        public bool Radial;
        public bool Floats;
        public readonly List<TrnFloraChild> Children = new List<TrnFloraChild>();
    }

    public sealed class TrnFloraChild
    {
        public string Name;
        public float Weight;
        public float Distance;
        public float MinimumWidth = 1;
        public float MaximumWidth = 1;
        public float MinimumHeight = 1;
        public float MaximumHeight = 1;
        public bool MaintainAspectRatio;
        public bool Sways;
        public float SwayDisplacement;
        public float SwayPeriod;
        public bool AlignToTerrain;
        public bool Crossed;
        public bool Scale;
        public float MinimumScale = 1;
        public float MaximumScale = 1;
    }
}
