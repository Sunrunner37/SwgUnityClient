namespace Swg.Client.Core.World.Terrain
{
    public readonly struct TerrainFloraSelection
    {
        public readonly int FamilyId;
        public readonly float ChildChoice;
        public TerrainFloraSelection(int familyId, float childChoice)
        {
            FamilyId = familyId;
            ChildChoice = childChoice;
        }
    }
}
