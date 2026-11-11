namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct CharacterEntry
    {
        public readonly string Name;
        public readonly uint Race;
        public readonly ulong SwgId;
        public readonly uint GalaxyId;
        public readonly uint ServerStatus;
        public CharacterEntry(string name, uint race, ulong swgId, uint galaxyId, uint serverStatus = 1)
        {
            Name = name;
            Race = race;
            SwgId = swgId;
            GalaxyId = galaxyId;
            ServerStatus = serverStatus;
        }
    }
}
