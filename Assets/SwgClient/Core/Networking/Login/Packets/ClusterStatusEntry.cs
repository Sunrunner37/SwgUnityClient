namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct ClusterStatusEntry
    {
        public readonly uint Id;
        public readonly string Address;
        public readonly ushort Port;
        public readonly ushort PingPort;
        public readonly uint Population;
        public readonly uint Reserved1;
        public readonly uint Reserved2;
        public readonly uint Reserved3;
        public readonly uint Status;
        public readonly byte ReservedByte;
        public ClusterStatusEntry(uint id, string address, ushort port, ushort pingPort, uint status = LoginClusterStatusPacket.OnlineStatus, uint population = 100, uint reserved1 = 0x00000cb2, uint reserved2 = 8, uint reserved3 = 0xffff8f80, byte reservedByte = 0)
        {
            Id = id;
            Address = address;
            Port = port;
            PingPort = pingPort;
            Status = status;
            Population = population;
            Reserved1 = reserved1;
            Reserved2 = reserved2;
            Reserved3 = reserved3;
            ReservedByte = reservedByte;
        }
    }
}
