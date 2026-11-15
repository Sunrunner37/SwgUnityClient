using Swg.Client.Core.Characters;
using Swg.Client.Core.Crafting;
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
        internal static WorldState CreateState(out CurrentPlayer player) => CreateState(new SceneObjectRegistry(), out player);
        internal static WorldState CreateState(SceneObjectRegistry registry, out CurrentPlayer player)
        {
            player = new CurrentPlayer(registry);
            return new WorldState(registry);
        }
        internal static GameReplication CreateReplication(WorldState state, CurrentPlayer currentPlayer)
        {
            var replication = new GameObject("Test replication").AddComponent<GameReplication>();
            replication.Initialize(state, currentPlayer, new CraftingProgress());
            replication.gameObject.SetActive(false);
            return replication;
        }

        internal static ILifetimeScope Create(GameReplication replication, CurrentPlayer currentPlayer)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(replication).ExternallyOwned();
            builder.RegisterInstance(replication.State);
            builder.RegisterInstance(currentPlayer);
            var progress = new CraftingProgress();
            replication.Initialize(replication.State, currentPlayer, progress);
            builder.RegisterInstance(progress);
            // Routing tests construct every handler but do not open a network connection.
            builder.Register(_ => new ClientCommands(_ => { }, _ => { }, currentPlayer)).SingleInstance();
            builder.Register(_ => new StringTableLocalizer(null)).SingleInstance();
            builder.RegisterType<ProseFormatter>().SingleInstance();
            builder.RegisterType<ClientFlyText>().SingleInstance();
            builder.Register(context => new ClientChat(null, replication.State, context.Resolve<StringTableLocalizer>(), context.Resolve<ProseFormatter>(), context.Resolve<ClientCommands>(), currentPlayer)).SingleInstance();
            // Crafting handlers resolve their session only when a crafting reply is applied on the main thread.
            builder.RegisterType<ClientCrafting>().SingleInstance();
            builder.RegisterType<ClientMissions>().SingleInstance();
            builder.RegisterInstance(new System.Action<Swg.Client.Core.Networking.Game.Packets.Mission.MissionListRequestPacket>(_ => { }));
            builder.RegisterInstance(new System.Action<Swg.Client.Core.Networking.Game.Packets.Mission.MissionAcceptPacket>(_ => { }));
            builder.RegisterInstance(new System.Action<Swg.Client.Core.Networking.Game.Packets.Mission.MissionAbortPacket>(_ => { }));
            builder.RegisterInstance(replication.SceneObjects);
            builder.Register(_ => new ClientInventory(replication.State, swgId => { }, currentPlayer)).SingleInstance();
            builder.Register(_ => new ClientObjectInteractions(replication.State, packet => { }, swgId => { }, packet => { }, (name, arguments, target) => { }, currentPlayer)).SingleInstance();
            builder.RegisterAssemblyTypes(typeof(GamePacketRouter).Assembly).Where(type => typeof(IGamePacketHandler).IsAssignableFrom(type) || typeof(IObjectControllerHandler).IsAssignableFrom(type)).AsSelf();
            return builder.Build();
        }
    }
}
