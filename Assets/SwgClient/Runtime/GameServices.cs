using Swg.Client.Core;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Networking.Game.Packets.Targeting;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Networking.Chat;
using Swg.Client.Core.Networking.Game.Handlers;
using System.IO;
using System;
using System.Threading;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.Login;
using System.Linq;
using Autofac;
using UnityEngine;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Audio;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.Assets.Shaders;

namespace Swg.Client.Runtime
{
    public static class GameServices
    {
        static IContainer global;
        static ILifetimeScope game;
        public static GameConnection Connection { get; private set; }
        internal static ILifetimeScope GameScope => game;

        public static T Resolve<T>() => (game ?? (ILifetimeScope)global).Resolve<T>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            string ini = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini");
            var root = new GameObject("Game services");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var music = root.AddComponent<ClientMusic>();
            root.GetComponent<AudioSource>().playOnAwake = false;
            var builder = new ContainerBuilder();
            builder.RegisterInstance(ClientSettings.From(ini));
            builder.RegisterType<TreRuntimeAssets>().SingleInstance();
            builder.RegisterType<StringTableLocalizer>().SingleInstance();
            builder.RegisterType<SceneObjectNameResolver>().SingleInstance();
            builder.RegisterType<ProseFormatter>().SingleInstance();
            builder.RegisterType<AudioClipLoader>().SingleInstance();
            builder.RegisterType<SwgMaterialFactory>().SingleInstance();
            builder.RegisterInstance(music).ExternallyOwned();
            builder.Register(_ => Connection ?? throw new InvalidOperationException("Log in before sending game packets.")).ExternallyOwned();
            global = builder.Build();
            Application.quitting += Shutdown;
        }

        internal static ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> register)
        {
            EndGame();
            game = global.BeginLifetimeScope(register);
            return game;
        }

        internal static void BeginGame(GameConnection connection)
        {
            // These singletons are owned by the current game lifetime.
            BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(connection).ExternallyOwned();
                builder.RegisterInstance(connection.Admission);
                builder.RegisterType<SceneObjectRegistry>().SingleInstance().OnRelease(value => value.Clear());
                builder.RegisterType<WorldState>().SingleInstance();
                builder.RegisterInstance(new Action<TargetUpdatePacket>(packet => connection.Send(packet)));
                builder.RegisterType<ClientTargeting>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterInstance(new Action<CommandQueueEnqueuePacket>(packet => connection.Send(packet)));
                builder.RegisterInstance(new Action<CommandQueueDequeuePacket>(packet => connection.Send(packet)));
                builder.Register(context =>
                {
                    var commands = context.Resolve<ClientCommands>();
                    return new Action<string, string, SwgId>((name, arguments, target) => commands.Enqueue(name, arguments, target));
                }).SingleInstance();
                builder.RegisterType<ClientDatapad>().AsSelf().SingleInstance();
                builder.RegisterType<ClientCombatQueue>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<ClientCommands>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterInstance(new Action<ObjectMenuRequestPacket>(packet => connection.Send(packet)));
                builder.Register(context =>
                {
                    var commands = context.Resolve<ClientCommands>();
                    return new Action<SwgId>(swgId => commands.Enqueue("getAttributesBatch", swgId.ToString()));
                }).SingleInstance();
                builder.RegisterType<ClientObjectInteractions>().AsSelf().SingleInstance();
                builder.Register(context =>
                {
                    var commands = context.Resolve<ClientCommands>();
                    return new ClientInventory(context.Resolve<WorldState>(), swgId => commands.Enqueue("openContainer", "", swgId));
                }).SingleInstance();
                builder.RegisterType<ClientChat>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<ClientFlyText>().SingleInstance();
                builder.RegisterType<CommandCatalog>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<CommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.Register(context =>
                {
                    var replication = new GameObject("Game replication").AddComponent<GameReplication>();
                    replication.Initialize(context.Resolve<WorldState>());
                    UnityEngine.Object.DontDestroyOnLoad(replication.gameObject);
                    return replication;
                }).AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<AppearanceCache>().SingleInstance();
                builder.RegisterType<WorldContentLoader>().SingleInstance();
                builder.RegisterType<WorldRenderingContext>().SingleInstance();
                builder.RegisterType<ItemPreviewCache>().SingleInstance();
                builder.Register(context =>
                {
                    var previews = new GameObject("Inventory previews").AddComponent<InventoryPreviewCache>();
                    previews.Initialize(context.Resolve<ClientInventory>(), context.Resolve<WorldContentLoader>(), context.Resolve<ItemPreviewCache>());
                    UnityEngine.Object.DontDestroyOnLoad(previews.gameObject);
                    return previews;
                }).SingleInstance();
                builder.RegisterType<WorldAppearanceLibrary>().SingleInstance();
                builder.RegisterType<WorldTerrainRenderer>().SingleInstance();
                builder.Register(context =>
                {
                    var rendering = context.Resolve<WorldRenderingContext>();
                    return new WorldStreamer(context.Resolve<WorldContentLoader>(), rendering.Streaming, context.Resolve<WorldAppearanceLibrary>(), context.Resolve<WorldTerrainRenderer>(), rendering.Renderer, rendering.Owner, rendering.FloraRenderer);
                }).SingleInstance();
                builder.RegisterAssemblyTypes(typeof(GamePacketRouter).Assembly).Where(type => typeof(IGamePacketHandler).IsAssignableFrom(type) || typeof(IObjectControllerHandler).IsAssignableFrom(type)).AsSelf().SingleInstance();
            });
        }

        internal static void AcceptConnection(GameConnection connection)
        {
            Disconnect();
            Connection = connection;
            connection.Disconnected += OnDisconnected;
            connection.Start(SynchronizationContext.Current);
        }

        static void OnDisconnected(string reason)
        {
            Debug.LogWarning("Disconnected: " + reason);
            Disconnect();
            ScreenChanger.Open(ScreenId.Login);
        }

        public static void Disconnect()
        {
            if (Connection != null)
            {
                Connection.Disconnected -= OnDisconnected;
                Connection.Dispose();
                Connection = null;
            }

            EndGame();
        }

        internal static void EndGame(ILifetimeScope session)
        {
            if (session != null && ReferenceEquals(game, session))
                EndGame();
        }

        internal static void EndGame()
        {
            if (game != null)
            {
                Connection?.SetReceiver(null);
                game.Dispose();
            }

            game = null;
            ManagedSceneObject.ResetPlayerCache();
        }

        internal static void Shutdown()
        {
            Application.quitting -= Shutdown;
            Disconnect();
            global?.Dispose();
            global = null;
        }
    }
}
