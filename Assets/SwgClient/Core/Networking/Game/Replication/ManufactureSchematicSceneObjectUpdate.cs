using System;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class ManufactureSchematicSceneObjectUpdate : SceneObjectUpdate
    {
        public uint? DraftSchematicCrc;
        public int? DataSize;
        public override SceneObjectChange Changes => base.Changes
            | (DraftSchematicCrc.HasValue ? SceneObjectChange.Appearance : SceneObjectChange.None)
            | (DataSize.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None);
        internal override Type RequiredType => typeof(ManufactureSchematicSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((ManufactureSchematicSceneObject)sceneObject).Apply(this);
    }
}
