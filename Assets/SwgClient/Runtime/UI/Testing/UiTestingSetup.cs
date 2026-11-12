using Swg.Client.Core.Commands;
using System;
using Autofac;
using UnityEngine;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.Commands;

namespace Swg.Client.Runtime.UI.Testing
{
    [DefaultExecutionOrder(-1000)]
    public sealed class UiTestingSetup : MonoBehaviour
    {
        public string[] sampleAbilities = Array.Empty<string>();
        PlayerCommands player;
        ILifetimeScope scope;

        void Awake()
        {
            GameServices.Disconnect();
            scope = GameServices.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<SceneObjectRegistry>().SingleInstance();
                builder.RegisterType<WorldState>().SingleInstance();
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.RegisterType<OfflineCommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
            });
            player = scope.Resolve<WorldState>().Commands;
            RefreshAbilities();
        }

        public void RefreshAbilities() => player.Apply(list =>
        {
            list.Clear();
            list.AddRange(sampleAbilities);
        });

        void OnDestroy() => GameServices.EndGame(scope);
    }
}
