using Swg.Client.Runtime.Networking;
using Autofac;
using UnityEngine;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Tests.Editor
{
    internal static class GameServicesForTests
    {
        internal static GameReplication CreateReplication(WorldState state)
        {
            var replication = new GameObject("Test replication").AddComponent<GameReplication>();
            replication.Initialize(state);
            replication.gameObject.SetActive(false);
            return replication;
        }

        internal static ILifetimeScope Create(GameReplication replication)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(replication).ExternallyOwned();
            builder.RegisterInstance(replication.State);
            builder.RegisterInstance(replication.SceneObjects);
            builder.RegisterAssemblyTypes(typeof(GamePacketRouter).Assembly).Where(type => typeof(IGamePacketHandler).IsAssignableFrom(type) || typeof(IObjectControllerHandler).IsAssignableFrom(type)).AsSelf();
            return builder.Build();
        }
    }
}
