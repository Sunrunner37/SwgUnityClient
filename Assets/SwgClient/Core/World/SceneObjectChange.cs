using System;

namespace Swg.Client.Core.World
{
    [Flags]
    public enum SceneObjectChange
    {
        None = 0,
        Transform = 1,
        Appearance = 2,
        Containment = 4,
        Replaced = 8,
        All = Transform | Appearance | Containment
    }
}
