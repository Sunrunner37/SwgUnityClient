using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    public readonly struct TerrainFloraPlacement
    {
        public readonly TerrainFloraItem Item;
        public readonly int Detail;
        public readonly string AppearanceKey;
        public readonly TerrainFloraKind Kind;
        public readonly Vector3 Normal;
        public readonly Vector3 ServerPosition;
        public readonly float Yaw;
        public readonly float Scale;
        public readonly Vector3 Size;
        public readonly Color Color;
        public TerrainFloraPlacement(TerrainFloraItem item, TerrainFloraKind kind, Vector3 serverPosition, Vector3 normal, float yaw, float scale, Vector3 size, Color color, int detail = 0, string appearanceKey = null)
        {
            Item = item;
            Detail = detail;
            AppearanceKey = appearanceKey ?? item.Path;
            Kind = kind;
            Normal = normal;
            ServerPosition = serverPosition;
            Yaw = yaw;
            Scale = scale;
            Size = size;
            Color = color;
        }
    }
}
