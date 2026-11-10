using Swg.Client.Core.Networking.Game.Packets.Scene;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game
{
    /// <summary>Thread-safe replicated state. Wire decoding belongs to the packet handlers.</summary>
    public sealed class GameWorldState
    {
        readonly object gate = new object ();
        readonly Dictionary<ulong, GameObjectState> objects = new Dictionary<ulong, GameObjectState>();
        CmdStartScenePacket? startScene;
        bool sceneReadyAcknowledged;
        long revision;
        public long Revision
        {
            get
            {
                lock (gate)
                    return revision;
            }
        }

        public GameSceneStatus CaptureSceneStatus()
        {
            lock (gate)
            {
                bool complete = startScene.HasValue && objects.TryGetValue(startScene.Value.PlayerId, out var player) && player.BaselinesComplete;
                return new GameSceneStatus(startScene, complete, sceneReadyAcknowledged);
            }
        }

        public GameWorldSnapshot CaptureSnapshot()
        {
            lock (gate)
            {
                bool complete = startScene.HasValue && objects.TryGetValue(startScene.Value.PlayerId, out var player) && player.BaselinesComplete;
                return new GameWorldSnapshot(startScene, complete, sceneReadyAcknowledged, revision, objects.Values.Select(o => o.Copy()).ToArray());
            }
        }

        internal void StartScene(CmdStartScenePacket start)
        {
            if (start.PlayerId == 0)
                throw new InvalidDataException("Zero replicated object ID");
            lock (gate)
            {
                objects.Clear();
                startScene = start;
                sceneReadyAcknowledged = false;
                objects.Add(start.PlayerId, new GameObjectState { Id = start.PlayerId, Position = start.Position, TemplatePath = start.TemplatePath, Revision = ++revision });
            }
        }

        internal void AcknowledgeSceneReady()
        {
            lock (gate)
            {
                sceneReadyAcknowledged = true;
                revision++;
            }
        }

        internal void Remove(ulong id)
        {
            lock (gate)
            {
                objects.Remove(id);
                revision++;
            }
        }

        // Updates contain decoded values only: no parsing or user callbacks run under this lock.
        internal void Commit(ulong id, GameObjectUpdate update)
        {
            if (id == 0)
                throw new InvalidDataException("Zero replicated object ID");
            lock (gate)
            {
                if (!objects.TryGetValue(id, out var value))
                {
                    value = new GameObjectState
                    {
                        Id = id
                    };
                    objects.Add(id, value);
                }

                update.ApplyTo(value);
                value.Revision = ++revision;
            }
        }
    }
}
