using Swg.Client.Core.Networking.Game.Replication;
using UnityEngine;

namespace Swg.Client.Core.World.SceneObjects
{
    public class SpatialSceneObject : SceneObject
    {
        // SWG coordinates: parent-relative indoors; absolute without a parent and for mounted riders.
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public float Scale = 1;
        internal void Apply(MovementUpdate update)
        {
            if (update.Position.HasValue)
                Position = update.Position.Value;
            if (update.Rotation.HasValue)
                Rotation = update.Rotation.Value;
        }

        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is SpatialSceneObject spatial)
            {
                spatial.Position = Position;
                spatial.Rotation = Rotation;
                spatial.Scale = Scale;
            }
        }
    }
}
