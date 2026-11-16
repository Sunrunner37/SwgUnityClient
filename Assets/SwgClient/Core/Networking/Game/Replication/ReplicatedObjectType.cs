namespace Swg.Client.Core.Networking.Game.Replication
{
    internal enum ReplicatedObjectType : uint
    {
        Tangible = 0x54414e4f,
        Creature = 0x4352454f,
        Building = 0x4255494f,
        Cell = 0x53434c54,
        Player = 0x504c4159,
        Group = 0x47525550,
        Guild = 0x47494c44,
        ManufactureSchematic = 0x4d53434f,
        ResourceContainer = 0x52434e4f,
        Intangible = 0x4f4e5449,
        Mission = 0x4d49534f
    }
}
