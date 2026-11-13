using UnityEngine;

namespace Swg.Client.Core.World
{
    public enum WaypointColor : byte
    {
        White,
        Blue,
        Green,
        Orange,
        Yellow,
        Purple
    }

    /// <summary>A player-owned navigation destination. Position is absolute SWG world space (Y is height).</summary>
    public readonly struct Waypoint
    {
        public SwgId SwgId { get; }
        public Vector3 Position { get; }
        public uint PlanetCrc { get; }
        public string Name { get; }
        public WaypointColor Color { get; }
        public bool Active { get; }

        public Waypoint(SwgId swgId, Vector3 position, uint planetCrc, string name, WaypointColor color, bool active)
        {
            SwgId = swgId;
            Position = position;
            PlanetCrc = planetCrc;
            Name = name;
            Color = color;
            Active = active;
        }
    }
}
