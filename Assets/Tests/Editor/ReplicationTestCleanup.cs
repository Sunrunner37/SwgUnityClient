using Swg.Client.Runtime.Networking;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Tests.Editor
{
    [SetUpFixture]
    public sealed class ReplicationTestCleanup
    {
        [OneTimeTearDown]
        public void DestroyTestComponents()
        {
            foreach (var replication in Object.FindObjectsByType<GameReplication>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (replication.name == "Test replication")
                    Object.DestroyImmediate(replication.gameObject);
        }
    }
}
