using System.Linq;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class GameObjectUpdate
    {
        public SwgId? ParentSwgId;
        public uint? TemplateCrc;
        public UnityEngine.Vector3? Position;
        public UnityEngine.Quaternion? Rotation;
        public uint? Arrangement;
        public uint? MovementCounter;
        public uint? BaselineType;
        public bool? BaselinesComplete;
        public string ResourceType;
        public int? Volume;
        public int? Quantity;
        public int? CashCredits;
        public int? BankCredits;
        public string Name;
        public byte[] Customization;
        public WorldEquipment[] Equipment;
        public EquipmentDelta EquipmentDelta;
        public string AlternateAppearance;
        public float? Height;
        public byte? Posture;
        public float? RunSpeed;
        public float? SpeedMultiplier;
        public float? TurnScale;
        public SwgId? TargetSwgId;
        public int[] Ham;
        public int[] MaxHam;
        public int[] Wounds;
        public IntVectorDelta HamDelta;
        public IntVectorDelta MaxHamDelta;
        public IntVectorDelta WoundsDelta;
        public SceneObjectChange Changes => (Position.HasValue || Rotation.HasValue || Height.HasValue ? SceneObjectChange.Transform : SceneObjectChange.None) | (TemplateCrc.HasValue || BaselineType.HasValue || BaselinesComplete.HasValue || Name != null || ResourceType != null || Arrangement.HasValue || Customization != null || Equipment != null || EquipmentDelta != null || AlternateAppearance != null || Posture.HasValue ? SceneObjectChange.Appearance : SceneObjectChange.None)
            | (Ham != null || MaxHam != null || Wounds != null || HamDelta != null || MaxHamDelta != null || WoundsDelta != null ? SceneObjectChange.Vitals : SceneObjectChange.None)
            | (Volume.HasValue || Quantity.HasValue || CashCredits.HasValue || BankCredits.HasValue ? SceneObjectChange.Inventory : SceneObjectChange.None)
            | (TargetSwgId.HasValue ? SceneObjectChange.Target : SceneObjectChange.None);

        public GameObjectUpdate CopyForQueue()
        {
            var copy = (GameObjectUpdate)MemberwiseClone();
            if (Customization != null)
                copy.Customization = (byte[])Customization.Clone();
            if (Equipment != null)
                copy.Equipment = Equipment.Select(item => new WorldEquipment((byte[])item.Customization.Clone(), item.Arrangement, item.SwgId, item.TemplateCrc)).ToArray();
            copy.Ham = Ham == null ? null : (int[])Ham.Clone();
            copy.MaxHam = MaxHam == null ? null : (int[])MaxHam.Clone();
            copy.Wounds = Wounds == null ? null : (int[])Wounds.Clone();
            return copy;
        }

        public void ApplyTo(SceneObject target)
        {
            if (TemplateCrc.HasValue)
                target.TemplateCrc = TemplateCrc.Value;
            if (Arrangement.HasValue)
                target.Arrangement = Arrangement.Value;
            if (Name != null)
                target.Name = Name;
            if (target is SpatialSceneObject spatial)
            {
                if (Position.HasValue)
                    spatial.Position = Position.Value;
                if (Rotation.HasValue)
                    spatial.Rotation = Rotation.Value;
                if (Height.HasValue)
                    spatial.Scale = Height.Value;
            }

            if (target is TangibleSceneObject tangible)
            {
                if (ResourceType != null)
                    tangible.ResourceType = ResourceType;
                if (Volume.HasValue)
                    tangible.Volume = Volume.Value;
                if (Quantity.HasValue)
                    tangible.Quantity = Quantity.Value;
                if (Customization != null)
                    tangible.Customization = Customization;
                if (AlternateAppearance != null)
                    tangible.AlternateAppearance = AlternateAppearance;
            }

            if (target is CreatureSceneObject creature)
            {
                if (CashCredits.HasValue)
                    creature.CashCredits = CashCredits.Value;
                if (BankCredits.HasValue)
                    creature.BankCredits = BankCredits.Value;
                if (TargetSwgId.HasValue)
                    creature.TargetSwgId = TargetSwgId.Value;
                HamPoolReplication.Apply(this, ref creature.Ham);
                if (RunSpeed.HasValue)
                    creature.RunSpeed = RunSpeed.Value;
                if (SpeedMultiplier.HasValue)
                    creature.SpeedMultiplier = SpeedMultiplier.Value;
                if (TurnScale.HasValue)
                    creature.TurnScale = TurnScale.Value;
                if (Equipment != null)
                    creature.Equipment = Equipment;
                if (EquipmentDelta != null)
                    creature.Equipment = EquipmentDelta.Apply(creature.Equipment);
                if (Posture.HasValue)
                    creature.Posture = (CreaturePosture)Posture.Value;
            }
        }
    }
}
