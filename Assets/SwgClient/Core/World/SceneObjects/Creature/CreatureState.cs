using System;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    // Core3 templates/params/creature/CreatureState.h. Preserve the complete replicated mask, including unmodeled flags.
    [Flags]
    public enum CreatureState : ulong
    {
        None = 0,
        Cover = 1,
        Combat = 2,
        Peace = 4,
        RidingMount = 0x8000000,
        MountedCreature = 0x10000000
    }
}
