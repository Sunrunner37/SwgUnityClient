namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct ClusterNameEntry
    {
        public readonly uint Id;
        public readonly string Name;
        public readonly uint Reserved;
        // Core3 writes this opaque value after every name; its meaning is not established.
        public ClusterNameEntry(uint id, string name, uint reserved = 0xffff8f80)
        {
            Id = id;
            Name = name;
            Reserved = reserved;
        }
    }
}
