using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using System.Collections.Generic;

namespace Swg.Client.Core.Networking.Game
{
    public readonly struct GameWorldSnapshot
    {
        public readonly CmdStartScenePacket? StartScene;
        public readonly ObjectId PlayerId;
        public readonly bool PlayerBaselinesComplete;
        public readonly bool SceneReadyAcknowledged;
        public readonly long Revision;
        public readonly IReadOnlyList<SceneObject> Objects;
        internal GameWorldSnapshot(CmdStartScenePacket? start, bool complete, bool acknowledged, long revision, SceneObject[] objects)
        {
            StartScene = start;
            PlayerId = start?.PlayerId ?? 0;
            PlayerBaselinesComplete = complete;
            SceneReadyAcknowledged = acknowledged;
            Revision = revision;
            Objects = objects;
        }
    }
}
