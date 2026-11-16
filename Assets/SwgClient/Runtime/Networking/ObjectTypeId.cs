namespace Swg.Client.Runtime.Networking
{
    /// <summary>SHOT game-object type identifiers used by object interactions, from Core3 SceneObjectType.h.
    /// Unlisted metadata values remain valid enum values; these are identifiers, not flags.</summary>
    public enum ObjectTypeId : int
    {
        Unknown = 0x0,
        Creature = 0x400,
        Mission = 0x803,
        PetControlDevice = 0x807,
        VehicleControlDevice = 0x808,
        DroidControlDevice = 0x80b,
        Container = 0x2005,
        CraftingStation = 0x2006,
        Food = 0x200a,
        Pharmaceutical = 0x200d,
        Sign = 0x200f,
        TravelTicket = 0x2012,
        Trap = 0x2014,
        WearableContainer = 0x2015,
        Drink = 0x2018,
        Firework = 0x2019,
        PetMedicine = 0x201b,
        FireworkShow = 0x201c,
        Container2 = 0x2027,
        StaticLootContainer = 0x2034,
        BankTerminal = 0x4001,
        CloningTerminal = 0x4003,
        InsuranceTerminal = 0x4004,
        MissionTerminal = 0x4006,
        NativeTravelTerminal = 0x400a,
        NativeTicketCollector = 0x400c, // Original SHOT type; Core3 specializes it as 0x4011.
        TicketCollector = 0x4011,
        TravelTerminal = 0x4012,
        CraftingTool = 0x8001,
        SurveyTool = 0x8002,
        RepairTool = 0x8003,
        CampKit = 0x8004,
        Vehicle = 0x10000,
        ThrownWeapon = 0x20003,
        HeavyWeapon = 0x20004,
        SpecialHeavyWeapon = 0x20006,
        BuildingDeed = 0x800001,
        InstallationDeed = 0x800002,
        ResourceDeed = 0x800006,
        VeteranHarvesterDeed = 0x800008,
    }
}
