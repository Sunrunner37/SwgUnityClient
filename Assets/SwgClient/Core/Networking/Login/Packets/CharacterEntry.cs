namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct CharacterEntry
    {
        public readonly string Name;
        public readonly uint Race;
        public readonly ulong ObjectId;
        public readonly uint GalaxyId;
        public readonly uint ServerStatus;
        public CharacterEntry(string name, uint race, ulong objectId, uint galaxyId, uint serverStatus = 1)
        {
            Name = name;
            Race = race;
            ObjectId = objectId;
            GalaxyId = galaxyId;
            ServerStatus = serverStatus;
        }
    }
}
