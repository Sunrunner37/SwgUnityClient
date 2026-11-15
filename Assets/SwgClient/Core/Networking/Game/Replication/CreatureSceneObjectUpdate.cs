using System;
using System.Linq;
using System.Collections.Generic;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Creature baseline/delta fields, including the tangible fields inherited on the wire.</summary>
    internal sealed class CreatureSceneObjectUpdate : TangibleSceneObjectUpdate
    {
        public int? CashCredits;
        public int? BankCredits;
        public WorldEquipment[] Equipment;
        public EquipmentDelta EquipmentDelta;
        public float? Height;
        public byte? Posture;
        public ulong? States;
        public float? AccelerationMultiplierBase;
        public float? AccelerationMultiplier;
        public float? RunSpeed;
        public float? SpeedMultiplier;
        public float? TurnScale;
        public SwgId? TargetSwgId;
        public SwgId? WeaponSwgId;
        public int? BattleFatigue;
        public HamPoolUpdate Ham;
        public Action<List<string>> Skills;
        public Action<Dictionary<string, int>> SkillModifiers;
        public override SceneObjectChange Changes => base.Changes
            | (Skills != null || SkillModifiers != null ? SceneObjectChange.Skills : SceneObjectChange.None)
            | (Height.HasValue ? SceneObjectChange.Transform : SceneObjectChange.None)
            | (Equipment != null || EquipmentDelta != null ? SceneObjectChange.Appearance : SceneObjectChange.None)
            | (BattleFatigue.HasValue || Ham?.HasChanges == true ? SceneObjectChange.Vitals : SceneObjectChange.None)
            | (CashCredits.HasValue || BankCredits.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None)
            | (States.HasValue || WeaponSwgId.HasValue ? SceneObjectChange.State : SceneObjectChange.None)
            | (Posture.HasValue ? SceneObjectChange.Posture : SceneObjectChange.None)
            | (TargetSwgId.HasValue ? SceneObjectChange.Target : SceneObjectChange.None);
        internal override Type RequiredType => typeof(CreatureSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((CreatureSceneObject)sceneObject).Apply(this);
        public override SceneObjectUpdate CopyForQueue()
        {
            var copy = (CreatureSceneObjectUpdate)base.CopyForQueue();
            if (Equipment != null)
                copy.Equipment = Equipment.Select(item => new WorldEquipment((byte[])item.Customization.Clone(), item.Arrangement, item.SwgId, item.TemplateCrc)).ToArray();
            copy.Ham = Ham?.CopyForQueue();
            return copy;
        }
    }
}
