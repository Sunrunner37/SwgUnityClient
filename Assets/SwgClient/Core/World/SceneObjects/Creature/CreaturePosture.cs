namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    // Core3 templates/params/creature/CreaturePosture.h; replicated as a single byte.
    public enum CreaturePosture : byte
    {
        Upright = 0,
        Crouched = 1,
        Prone = 2,
        Sneaking = 3,
        Blocking = 4,
        Climbing = 5,
        Flying = 6,
        LyingDown = 7,
        Sitting = 8,
        SkillAnimating = 9,
        DrivingVehicle = 10,
        RidingCreature = 11,
        KnockedDown = 12,
        Incapacitated = 13,
        Dead = 14,
        Invalid = 255
    }
}
