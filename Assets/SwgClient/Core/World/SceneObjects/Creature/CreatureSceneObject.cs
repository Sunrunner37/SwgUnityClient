using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using System;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    public sealed class CreatureSceneObject : TangibleSceneObject
    {
        public int CashCredits;
        public int BankCredits;
        public WorldEquipment[] Equipment = Array.Empty<WorldEquipment>();
        public CreaturePosture Posture;
        public CreatureState States;
        public bool IsInCombat => (States & CreatureState.Combat) != 0;
        public SwgId TargetSwgId;
        public HamPool Ham;
        public float RunSpeed;
        public float SpeedMultiplier = 1;
        public float TurnScale = 1;
        // Matches Core3 PlayerManager's allowed speed: getRunSpeed() * getSpeedMultiplierMod().
        public float MovementSpeed => Math.Max(0, RunSpeed * SpeedMultiplier);

        internal override void Apply(GameObjectUpdate update)
        {
            base.Apply(update);
            if (update.CashCredits.HasValue)
                CashCredits = update.CashCredits.Value;
            if (update.BankCredits.HasValue)
                BankCredits = update.BankCredits.Value;
            if (update.TargetSwgId.HasValue)
                TargetSwgId = update.TargetSwgId.Value;
            HamPoolReplication.Apply(update, ref Ham);
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
