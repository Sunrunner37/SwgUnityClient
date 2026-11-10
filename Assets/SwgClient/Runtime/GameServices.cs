using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Handlers;
using System.IO;
using System.Linq;
using Autofac;
using UnityEngine;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.Assets.Models;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Audio;
using Swg.Client.Runtime.World;

namespace Swg.Client.Runtime
{
    public static class GameServices
    {
        static IContainer global;
        static ILifetimeScope game;
        internal static ILifetimeScope GameScope => game;

        public static T Resolve<T>() => (game ?? (ILifetimeScope)global).Resolve<T>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            string ini = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini");
            var root = new GameObject("Game services");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var music = root.AddComponent<ClientMusic>();
            var connection = root.AddComponent<ClientConnection>();
            root.GetComponent<AudioSource>().playOnAwake = false;
            var builder = new ContainerBuilder();
            builder.RegisterInstance(ClientSettings.From(ini));
            builder.RegisterType<TreRuntimeAssets>().SingleInstance();
            builder.RegisterType<AudioClipLoader>().SingleInstance();
            builder.RegisterInstance(music).ExternallyOwned();
            builder.RegisterInstance(connection).ExternallyOwned();
            global = builder.Build();
            Application.quitting += Shutdown;
        }

        internal static void BeginGame()
        {
            game = global.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<SceneObjectRegistry>().InstancePerLifetimeScope().OnRelease(value => value.Clear());
                builder.RegisterType<WorldState>().InstancePerLifetimeScope();
                builder.Register(context =>
                {
                    var replication = new GameObject("Game replication").AddComponent<GameReplication>();
                    replication.Initialize(context.Resolve<WorldState>());
                    UnityEngine.Object.DontDestroyOnLoad(replication.gameObject);
                    return replication;
                }).AsSelf().InstancePerLifetimeScope().AutoActivate();
                builder.RegisterType<ModelCache>().InstancePerLifetimeScope();
                builder.Register(context =>
                {
                    var settings = context.Resolve<ClientSettings>();
                    var connection = context.Resolve<ClientConnection>();
                    return new WorldContentLoader(context.Resolve<TreRuntimeAssets>(), context.Resolve<WorldState>(), context.Resolve<ModelCache>(), settings.AppearanceCacheDirectory, settings.LoginEndpoint, connection.Username, connection.Admission.Galaxy.Name);
                }).InstancePerLifetimeScope();
                builder.RegisterAssemblyTypes(typeof(GamePacketRouter).Assembly).Where(type => typeof(IGamePacketHandler).IsAssignableFrom(type) || typeof(IObjectControllerHandler).IsAssignableFrom(type)).AsSelf().InstancePerLifetimeScope();
            });
        }

        internal static void EndGame()
        {
            game?.Dispose();
            game = null;
        }

        internal static void Shutdown()
        {
            Application.quitting -= Shutdown;
            if (global != null)
                global.Resolve<ClientConnection>().Disconnect();
            global?.Dispose();
            global = null;
        }
    }
}
