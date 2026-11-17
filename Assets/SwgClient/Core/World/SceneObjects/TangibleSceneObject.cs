using Swg.Client.Core.Networking.Game.Replication;
using System;

namespace Swg.Client.Core.World.SceneObjects
{
    public class TangibleSceneObject : SpatialSceneObject
    {
        // Core3 CreatureFlag::ATTACKABLE and OptionBitmask::CONVERSE; both are server supplied.
        public uint PvpStatus;
        public uint Options;
        public bool CanAttack => (PvpStatus & 1) != 0;
        // Core3 CreatureFlag::AGGRESSIVE (0x02) or ENEMY (0x20), relative to this client.
        public bool IsHostile => (PvpStatus & 0x22) != 0;
        public bool CanConverse => (Options & 8) != 0;
        public string ResourceType;
        public int Volume = 1;
        public int ConditionDamage;
        public int MaximumCondition;
        public float ConditionFraction => MaximumCondition > 0 ? UnityEngine.Mathf.Clamp01(1f - (float)ConditionDamage / MaximumCondition) : 0;
        public int Quantity = 1;
        public byte[] Customization = Array.Empty<byte>();
        public string AlternateAppearance = "";
        internal void Apply(TangibleSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.PvpStatus.HasValue) PvpStatus = update.PvpStatus.Value;
            if (update.Options.HasValue) Options = update.Options.Value;
            if (update.ConditionDamage.HasValue) ConditionDamage = update.ConditionDamage.Value;
            if (update.MaximumCondition.HasValue) MaximumCondition = update.MaximumCondition.Value;
            if (update.ResourceType != null)
                ResourceType = update.ResourceType;
            if (update.Volume.HasValue)
                Volume = update.Volume.Value;
            if (update.Quantity.HasValue)
                Quantity = update.Quantity.Value;
            if (update.Customization != null)
                Customization = update.Customization;
            if (update.AlternateAppearance != null)
                AlternateAppearance = update.AlternateAppearance;
        }

        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is TangibleSceneObject tangible)
            {
                tangible.PvpStatus = PvpStatus;
                tangible.Options = Options;
                tangible.ResourceType = ResourceType;
                tangible.Volume = Volume;
                tangible.ConditionDamage = ConditionDamage;
                tangible.MaximumCondition = MaximumCondition;
                tangible.Quantity = Quantity;
                tangible.Customization = (byte[])Customization.Clone();
                tangible.AlternateAppearance = AlternateAppearance;
            }
        }

        internal override SceneObject Copy()
        {
            var copy = (TangibleSceneObject)base.Copy();
            copy.Customization = (byte[])Customization.Clone();
            return copy;
        }
    }
}
