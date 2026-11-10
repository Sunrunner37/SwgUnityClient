namespace Swg.Client.Core.Networking.Game.Replication
{
    internal enum ReplicatedObjectType : uint
    {
        Tangible = 0x54414e4f,
        Creature = 0x4352454f,
        Building = 0x4255494f,
        Cell = 0x43454c4c,
        Player = 0x504c4159,
        Guild = 0x47494c44,
        ResourceContainer = 0x52434e4f,
        Intangible = 0x49544e4f,
        Mission = 0x4d49534f
    }
}
