using System;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Tangible fields; specialized object updates inherit these when their packets include them.</summary>
    internal class TangibleSceneObjectUpdate : SceneObjectUpdate
    {
        public uint? PvpStatus;
        public uint? Options;
        public string ResourceType;
        public int? Volume;
        public int? ConditionDamage;
        public int? MaximumCondition;
        public int? Quantity;
        public byte[] Customization;
        public string AlternateAppearance;
        public override SceneObjectChange Changes => base.Changes
            | (PvpStatus.HasValue || Options.HasValue ? SceneObjectChange.State : SceneObjectChange.None)
            | (ResourceType != null || Customization != null || AlternateAppearance != null ? SceneObjectChange.Appearance : SceneObjectChange.None)
            | (ConditionDamage.HasValue || MaximumCondition.HasValue ? SceneObjectChange.Vitals : SceneObjectChange.None)
            | (Volume.HasValue || Quantity.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None);
        internal override Type RequiredType => typeof(TangibleSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((TangibleSceneObject)sceneObject).Apply(this);
        public override SceneObjectUpdate CopyForQueue()
        {
            var copy = (TangibleSceneObjectUpdate)base.CopyForQueue();
            if (Customization != null)
                copy.Customization = (byte[])Customization.Clone();
            return copy;
        }
    }
}
