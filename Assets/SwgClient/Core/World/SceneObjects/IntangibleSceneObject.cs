using Swg.Client.Core.Networking.Game.Replication;
namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>Datapad objects carry a replicated status; control devices use status 1 while their object is called.</summary>
    public sealed class IntangibleSceneObject : SceneObject
    {
        public int Status { get; private set; }
        public bool IsActive => Status == 1;
        internal void Apply(IntangibleSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.Status.HasValue) Status = update.Status.Value;
        }
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is IntangibleSceneObject intangible) intangible.Status = Status;
        }
    }
}
