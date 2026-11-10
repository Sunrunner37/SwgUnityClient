using Swg.Client.Core.Networking.Game.Packets.Scene;
using System;
using System.Collections.Generic;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Game
{
    public readonly struct GameWorldSnapshot
    {
        public readonly CmdStartScenePacket? StartScene;
        public readonly ulong PlayerId;
        public readonly bool PlayerBaselinesComplete;
        public readonly bool SceneReadyAcknowledged;
        public readonly long Revision;
        public readonly IReadOnlyList<GameObjectState> Objects;
        internal GameWorldSnapshot(CmdStartScenePacket? start, bool complete, bool acknowledged, long revision, GameObjectState[] objects)
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
