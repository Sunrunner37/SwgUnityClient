using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    public readonly struct TerrainFloraPlacement
    {
        public readonly TerrainFloraItem Item;
        public readonly TerrainFloraKind Kind;
        public readonly Vector3 Normal;
        public readonly Vector3 ServerPosition;
        public readonly float Yaw;
        public readonly float Scale;
        public TerrainFloraPlacement(TerrainFloraItem item, TerrainFloraKind kind, Vector3 serverPosition, Vector3 normal, float yaw, float scale)
        {
            Item = item;
            Kind = kind;
            Normal = normal;
            ServerPosition = serverPosition;
            Yaw = yaw;
            Scale = scale;
        }
    }
}
