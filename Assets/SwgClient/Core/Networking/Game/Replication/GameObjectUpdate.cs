using System.Linq;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class GameObjectUpdate
    {
        public ObjectId? ParentId;
        public uint? TemplateCrc;
        public UnityEngine.Vector3? Position;
        public UnityEngine.Quaternion? Rotation;
        public uint? Arrangement;
        public uint? MovementCounter;
        public uint? BaselineType;
        public bool? BaselinesComplete;
        public string Name;
        public byte[] Customization;
        public WorldEquipment[] Equipment;
        public string AlternateAppearance;
        public float? Height;
        public byte? Posture;
        public SceneObjectChange Changes => (Position.HasValue || Rotation.HasValue || Height.HasValue ? SceneObjectChange.Transform : SceneObjectChange.None) | (TemplateCrc.HasValue || BaselineType.HasValue || BaselinesComplete.HasValue || Name != null || Arrangement.HasValue || Customization != null || Equipment != null || AlternateAppearance != null || Posture.HasValue ? SceneObjectChange.Appearance : SceneObjectChange.None);

        public GameObjectUpdate CopyForQueue()
        {
            var copy = (GameObjectUpdate)MemberwiseClone();
            if (Customization != null)
                copy.Customization = (byte[])Customization.Clone();
            if (Equipment != null)
                copy.Equipment = Equipment.Select(item => new WorldEquipment((byte[])item.Customization.Clone(), item.Arrangement, item.ObjectId, item.TemplateCrc)).ToArray();
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
                if (Customization != null)
                    tangible.Customization = Customization;
                if (AlternateAppearance != null)
                    tangible.AlternateAppearance = AlternateAppearance;
            }

            if (target is CreatureSceneObject creature)
            {
                if (Equipment != null)
                    creature.Equipment = Equipment;
                if (Posture.HasValue)
                    creature.Posture = Posture.Value;
            }
        }
    }
}
