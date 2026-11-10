using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game
{
    /// <summary>Readiness metadata without cloning the replicated object collection.</summary>
    public readonly struct GameSceneStatus
    {
        public readonly CmdStartScenePacket? StartScene;
        public readonly bool PlayerBaselinesComplete;
        public readonly bool SceneReadyAcknowledged;
        internal GameSceneStatus(CmdStartScenePacket? startScene, bool complete, bool acknowledged)
        {
            StartScene = startScene;
            PlayerBaselinesComplete = complete;
            SceneReadyAcknowledged = acknowledged;
        }
    }
}
