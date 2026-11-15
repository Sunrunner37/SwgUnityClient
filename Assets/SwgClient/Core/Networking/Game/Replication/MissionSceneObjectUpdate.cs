using System;
using UnityEngine;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class MissionSceneObjectUpdate : SceneObjectUpdate
    {
        public int? Difficulty;
        public Vector3? StartPosition;
        public Vector3? EndPosition;
        public uint? StartPlanetCrc;
        public uint? EndPlanetCrc;
        public string Creator;
        public int? Reward;
        public uint? TargetTemplateCrc;
        public string Description;
        public string Title;
        public int? RefreshCounter;
        public uint? MissionTypeCrc;
        public string TargetName;
        public bool HasWaypoint;
        public Waypoint? Waypoint;
        public override SceneObjectChange Changes => base.Changes | SceneObjectChange.Inventory
            | (HasWaypoint ? SceneObjectChange.Waypoint : SceneObjectChange.None);
        internal override Type RequiredType => typeof(MissionSceneObject);
        internal override void ApplyTo(SceneObject sceneObject) => ((MissionSceneObject)sceneObject).Apply(this);
    }
}
