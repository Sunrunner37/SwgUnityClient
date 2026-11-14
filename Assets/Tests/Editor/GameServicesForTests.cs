using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking.Chat;
using Autofac;
using UnityEngine;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Handlers;
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
            // Routing tests construct every handler but do not open a network connection.
            builder.Register(_ => new ClientCommands(_ => { }, _ => { }, replication.State)).SingleInstance();
            builder.Register(_ => new StringTableLocalizer(null)).SingleInstance();
            builder.RegisterType<ProseFormatter>().SingleInstance();
            builder.RegisterType<ClientFlyText>().SingleInstance();
            builder.Register(context => new ClientChat(null, replication.State, context.Resolve<StringTableLocalizer>(), context.Resolve<ProseFormatter>(), context.Resolve<ClientCommands>())).SingleInstance();
            builder.RegisterInstance(replication.SceneObjects);
            builder.Register(_ => new ClientInventory(replication.State, swgId => { })).SingleInstance();
            builder.Register(_ => new ClientObjectInteractions(replication.State, packet => { }, swgId => { }, packet => { }, (name, arguments, target) => { })).SingleInstance();
            builder.RegisterAssemblyTypes(typeof(GamePacketRouter).Assembly).Where(type => typeof(IGamePacketHandler).IsAssignableFrom(type) || typeof(IObjectControllerHandler).IsAssignableFrom(type)).AsSelf();
            return builder.Build();
        }
    }
}
