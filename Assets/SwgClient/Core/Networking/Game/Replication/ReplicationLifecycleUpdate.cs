namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Creation/type metadata and baseline completion; these are replication concerns, not object attributes.</summary>
    internal readonly struct ReplicationLifecycleUpdate
    {
        public readonly uint? TemplateCrc;
        public readonly ReplicatedObjectType? ObjectType;
        public readonly bool BaselinesComplete;
        public ReplicationLifecycleUpdate(uint? templateCrc = null, ReplicatedObjectType? objectType = null, bool baselinesComplete = false)
        {
            TemplateCrc = templateCrc;
            ObjectType = objectType;
            BaselinesComplete = baselinesComplete;
        }
    }
}
