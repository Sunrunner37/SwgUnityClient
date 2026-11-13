using System;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Common object fields shared by baseline and delta packets.</summary>
    internal class SceneObjectUpdate
    {
        public string Name;
        public virtual SceneObjectChange Changes => Name != null ? SceneObjectChange.Appearance : SceneObjectChange.None;
        internal virtual Type RequiredType => null;
        internal virtual void ApplyTo(SceneObject sceneObject) => sceneObject.Apply(this);
        public virtual SceneObjectUpdate CopyForQueue() => (SceneObjectUpdate)MemberwiseClone();
    }
}
