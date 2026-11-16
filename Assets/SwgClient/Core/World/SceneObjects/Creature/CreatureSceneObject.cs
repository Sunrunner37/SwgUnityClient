using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Collections.Generic;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    public sealed class CreatureSceneObject : TangibleSceneObject
    {
        public int CashCredits;
        public int BankCredits;
        public WorldEquipment[] Equipment = Array.Empty<WorldEquipment>();
        public CreaturePosture Posture;
        public CreatureState States;
        public CreatureSceneObject Mount => Parent as CreatureSceneObject;
        public bool IsInCombat => (States & CreatureState.Combat) != 0;
        public SwgId TargetSwgId;
        public SwgId WeaponSwgId;
        public SwgId GroupSwgId;
        public HamPool Ham;
        public string PerformanceAnimation = "";
        public string MoodAnimation = "";
        public int PerformanceType;
        public SwgId ListenSwgId;
        public bool IsPlayingMusic => PerformanceType > 0 && PerformanceAnimation.StartsWith("music_", StringComparison.Ordinal);
        public bool IsDancing => PerformanceType > 0 && PerformanceAnimation.StartsWith("dance_", StringComparison.Ordinal);
        public int BattleFatigue;
        public List<string> Skills = new List<string>();
        public Dictionary<string, int> SkillModifiers = new Dictionary<string, int>();
        public float AccelerationMultiplierBase = 1;
        public float AccelerationMultiplier = 1;
        public float RunSpeed;
        public float SpeedMultiplier = 1;
        public float TurnScale = 1;
        // Matches Core3 PlayerManager's allowed speed: getRunSpeed() * getSpeedMultiplierMod().
        public float MovementSpeed => Math.Max(0, RunSpeed * SpeedMultiplier);

        internal void Apply(CreatureSceneObjectUpdate update)
        {
            base.Apply(update);
            update.Skills?.Invoke(Skills);
            update.SkillModifiers?.Invoke(SkillModifiers);
            if (update.Height.HasValue)
                Scale = update.Height.Value;
            if (update.CashCredits.HasValue)
                CashCredits = update.CashCredits.Value;
            if (update.BankCredits.HasValue)
                BankCredits = update.BankCredits.Value;
            if (update.PerformanceAnimation != null) PerformanceAnimation = update.PerformanceAnimation;
            if (update.MoodAnimation != null) MoodAnimation = update.MoodAnimation;
            if (update.PerformanceType.HasValue) PerformanceType = update.PerformanceType.Value;
            if (update.ListenSwgId.HasValue) ListenSwgId = update.ListenSwgId.Value;
            if (update.GroupSwgId.HasValue) GroupSwgId = update.GroupSwgId.Value;
            if (update.WeaponSwgId.HasValue) WeaponSwgId = update.WeaponSwgId.Value;
            if (update.TargetSwgId.HasValue)
                TargetSwgId = update.TargetSwgId.Value;
            if (update.Ham != null)
                HamPoolReplication.Apply(update.Ham, ref Ham);
            if (update.BattleFatigue.HasValue) BattleFatigue = update.BattleFatigue.Value;
            if (update.AccelerationMultiplierBase.HasValue)
                AccelerationMultiplierBase = update.AccelerationMultiplierBase.Value;
            if (update.AccelerationMultiplier.HasValue)
                AccelerationMultiplier = update.AccelerationMultiplier.Value;
            if (update.RunSpeed.HasValue)
                RunSpeed = update.RunSpeed.Value;
            if (update.SpeedMultiplier.HasValue)
                SpeedMultiplier = update.SpeedMultiplier.Value;
            if (update.TurnScale.HasValue)
                TurnScale = update.TurnScale.Value;
            if (update.Equipment != null)
                Equipment = update.Equipment;
            if (update.EquipmentDelta != null)
                Equipment = update.EquipmentDelta.Apply(Equipment);
            if (update.States.HasValue)
                States = (CreatureState)update.States.Value;
            if (update.Posture.HasValue)
                Posture = (CreaturePosture)update.Posture.Value;
        }

        internal override SceneObject Copy()
        {
            var copy = (CreatureSceneObject)base.Copy();
            copy.Skills = new List<string>(Skills);
            copy.SkillModifiers = new Dictionary<string, int>(SkillModifiers);
            copy.Equipment = new WorldEquipment[Equipment.Length];
            for (int i = 0; i < Equipment.Length; i++)
            {
                var item = Equipment[i];
                copy.Equipment[i] = new WorldEquipment((byte[])item.Customization.Clone(), item.Arrangement, item.SwgId, item.TemplateCrc);
            }

            return copy;
        }
    }
}
