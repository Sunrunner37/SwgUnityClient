using System;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class IntangibleSceneObjectUpdate : SceneObjectUpdate
    {
        public int? Status;
        public override SceneObjectChange Changes => base.Changes | (Status.HasValue ? SceneObjectChange.State | SceneObjectChange.Inventory : SceneObjectChange.None);
        internal override Type RequiredType => typeof(IntangibleSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((IntangibleSceneObject)sceneObject).Apply(this);
    }
}
