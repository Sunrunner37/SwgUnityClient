using Swg.Client.Core.Networking.Game.Replication;
namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>A stored manufacturing schematic references a draft and consumes datapad space.</summary>
    public sealed class ManufactureSchematicSceneObject : SceneObject
    {
        public uint DraftSchematicCrc;
        public int DataSize = 1;
        internal void Apply(ManufactureSchematicSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.DraftSchematicCrc.HasValue) DraftSchematicCrc = update.DraftSchematicCrc.Value;
            if (update.DataSize.HasValue) DataSize = update.DataSize.Value;
        }
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is ManufactureSchematicSceneObject schematic)
            {
                schematic.DraftSchematicCrc = DraftSchematicCrc;
                schematic.DataSize = DataSize;
            }
        }
    }
}
