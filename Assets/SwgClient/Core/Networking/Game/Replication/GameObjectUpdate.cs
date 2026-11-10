namespace Swg.Client.Core.Networking.Game.Replication
{
    // A packet is fully decoded before these owned values are committed atomically.
    internal sealed class GameObjectUpdate
    {
        public ulong? ParentId;
        public uint? TemplateCrc;
        public UnityEngine.Vector3? Position;
        public UnityEngine.Quaternion? Rotation;
        public uint? Arrangement;
        public uint? MovementCounter;
        public uint? BaselineType;
        public bool? BaselinesComplete;
        public string Name;
        public byte[] Customization;
        public GameEquipment[] Equipment;
        public string AlternateAppearance;
        public float? Height;
        public byte? Posture;
        public void ApplyTo(GameObjectState target)
        {
            if (ParentId.HasValue)
                target.ParentId = ParentId.Value;
            if (TemplateCrc.HasValue)
                target.TemplateCrc = TemplateCrc.Value;
            if (Position.HasValue)
                target.Position = Position.Value;
            if (Rotation.HasValue)
                target.Rotation = Rotation.Value;
            if (Arrangement.HasValue)
                target.Arrangement = Arrangement.Value;
            if (MovementCounter.HasValue)
                target.MovementCounter = MovementCounter.Value;
            if (BaselineType.HasValue)
                target.BaselineType = BaselineType.Value;
            if (BaselinesComplete.HasValue)
                target.BaselinesComplete = BaselinesComplete.Value;
            if (Name != null)
                target.Name = Name;
            if (Customization != null)
                target.Customization = Customization;
            if (Equipment != null)
                target.Equipment = Equipment;
            if (AlternateAppearance != null)
                target.AlternateAppearance = AlternateAppearance;
            if (Height.HasValue)
                target.Height = Height.Value;
            if (Posture.HasValue)
                target.Posture = Posture.Value;
        }
    }
}
