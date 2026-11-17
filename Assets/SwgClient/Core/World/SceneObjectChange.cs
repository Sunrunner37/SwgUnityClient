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
        Inventory = 64,
        Posture = 128,
        State = 256,
        Skills = 512,
        Waypoint = 1024,
        Group = 2048,
        Performance = 4096,
        // Explicit authoritative corrections snap prediction; ordinary transform notifications interpolate.
        Correction = 8192,
        All = Transform | Appearance | Containment | Vitals | Target | Inventory | Posture | State | Skills | Waypoint | Group | Performance
    }
}
