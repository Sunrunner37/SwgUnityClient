using UnityEngine;

namespace Swg.Client.Core.World.SceneObjects
{
    public class SpatialSceneObject : SceneObject
    {
        // SWG coordinates: relative to the parent, or absolute server world coordinates without a parent.
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public float Scale = 1;
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
