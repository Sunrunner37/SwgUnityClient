namespace Swg.Client.Runtime.Networking
{
    public static class ObjectTypeIdExtensions
    {
        // Creature subtypes include 0x401-0x409; Core3 also defines 0x40A, 0x40D-0x40F.
        // Mask the subtype byte to match their common 0x400 creature category.
        public static bool IsCreature(this ObjectTypeId objectType) => ((int)objectType & ~0xff) == (int)ObjectTypeId.Creature;

        public static bool IsContainer(this ObjectTypeId objectType) => objectType == ObjectTypeId.Container || objectType == ObjectTypeId.WearableContainer || objectType == ObjectTypeId.Container2 || objectType == ObjectTypeId.StaticLootContainer;
        public static bool IsConsumable(this ObjectTypeId objectType) => objectType == ObjectTypeId.Food || objectType == ObjectTypeId.Drink;
        public static bool IsVehicle(this ObjectTypeId objectType) => ((int)objectType & ~0xff) == (int)ObjectTypeId.Vehicle;
        public static bool IsMissionTerminal(this ObjectTypeId objectType) => objectType == ObjectTypeId.MissionTerminal;
        public static bool IsCrafting(this ObjectTypeId objectType) => objectType == ObjectTypeId.CraftingTool || objectType == ObjectTypeId.CraftingStation;
        public static bool IsControlDevice(this ObjectTypeId objectType) => objectType == ObjectTypeId.PetControlDevice || objectType == ObjectTypeId.DroidControlDevice || objectType == ObjectTypeId.VehicleControlDevice;
    }
}
