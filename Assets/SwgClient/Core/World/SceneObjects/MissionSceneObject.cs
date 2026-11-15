using UnityEngine;
using Swg.Client.Core.Networking.Game.Replication;
namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>A replicated mission, offered in the mission bag or accepted in the datapad.</summary>
    public sealed class MissionSceneObject : SceneObject
    {
        public int Difficulty;
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public uint StartPlanetCrc;
        public uint EndPlanetCrc;
        public string Creator = "";
        public int Reward;
        public uint TargetTemplateCrc;
        public string Description = "";
        public string Title = "";
        public int RefreshCounter;
        public uint MissionTypeCrc;
        public string TargetName = "";
        public Waypoint? Waypoint;
        internal void Apply(MissionSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.Difficulty.HasValue) Difficulty = update.Difficulty.Value;
            if (update.StartPosition.HasValue) StartPosition = update.StartPosition.Value;
            if (update.EndPosition.HasValue) EndPosition = update.EndPosition.Value;
            if (update.StartPlanetCrc.HasValue) StartPlanetCrc = update.StartPlanetCrc.Value;
            if (update.EndPlanetCrc.HasValue) EndPlanetCrc = update.EndPlanetCrc.Value;
            if (update.Creator != null) Creator = update.Creator;
            if (update.Reward.HasValue) Reward = update.Reward.Value;
            if (update.TargetTemplateCrc.HasValue) TargetTemplateCrc = update.TargetTemplateCrc.Value;
            if (update.Description != null) Description = update.Description;
            if (update.Title != null) Title = update.Title;
            if (update.RefreshCounter.HasValue) RefreshCounter = update.RefreshCounter.Value;
            if (update.MissionTypeCrc.HasValue) MissionTypeCrc = update.MissionTypeCrc.Value;
            if (update.TargetName != null) TargetName = update.TargetName;
            if (update.HasWaypoint) Waypoint = update.Waypoint;
        }
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is not MissionSceneObject mission) return;
            mission.Difficulty = Difficulty;
            mission.StartPosition = StartPosition;
            mission.EndPosition = EndPosition;
            mission.StartPlanetCrc = StartPlanetCrc;
            mission.EndPlanetCrc = EndPlanetCrc;
            mission.Creator = Creator;
            mission.Reward = Reward;
            mission.TargetTemplateCrc = TargetTemplateCrc;
            mission.Description = Description;
            mission.Title = Title;
            mission.RefreshCounter = RefreshCounter;
            mission.MissionTypeCrc = MissionTypeCrc;
            mission.TargetName = TargetName;
            mission.Waypoint = Waypoint;
        }
    }
}
