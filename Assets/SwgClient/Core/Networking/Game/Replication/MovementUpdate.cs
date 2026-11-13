using UnityEngine;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>SWG coordinates, relative to the parent when supplied by the packet.</summary>
    internal readonly struct MovementUpdate
    {
        public readonly Vector3? Position;
        public readonly Quaternion? Rotation;
        public readonly SwgId? ParentSwgId;
        public readonly uint? MovementCounter;
        public MovementUpdate(Vector3? position = null, Quaternion? rotation = null, SwgId? parentSwgId = null, uint? movementCounter = null)
        {
            Position = position;
            Rotation = rotation;
            ParentSwgId = parentSwgId;
            MovementCounter = movementCounter;
        }
    }
}
