using System;
using System.Collections.Generic;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class ManufactureSchematicSceneObjectUpdate : SceneObjectUpdate
    {
        public uint? DraftSchematicCrc;
        public int? DataSize;
        public float? Complexity;
        public readonly List<Action<ManufactureSchematicSceneObject>> Crafting = new List<Action<ManufactureSchematicSceneObject>>();
        public override SceneObjectChange Changes => base.Changes
            | (DraftSchematicCrc.HasValue ? SceneObjectChange.Appearance : SceneObjectChange.None)
            | (Crafting.Count > 0 || Complexity.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None)
            | (DataSize.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None);
        internal override Type RequiredType => typeof(ManufactureSchematicSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((ManufactureSchematicSceneObject)sceneObject).Apply(this);
    }
}
