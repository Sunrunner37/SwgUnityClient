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
        Vitals = 16,
        Target = 32,
        All = Transform | Appearance | Containment | Vitals | Target
    }
}
