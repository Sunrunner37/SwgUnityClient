using Swg.Client.Core.Networking.Game.Packets.Targeting;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Core.Commands;
using System;
using Autofac;
using UnityEngine;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.Commands;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

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
                builder.RegisterInstance(new Action<TargetUpdatePacket>(packet => Debug.Log("Offline target: " + packet.TargetSwgId)));
                builder.RegisterType<ClientTargeting>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.RegisterType<OfflineCommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
            });
            var state = scope.Resolve<WorldState>();
            state.BeginScene(9000, Vector3.zero, "terrain/tatooine.trn");
            state.UpdateWaypoints(list =>
            {
                list[9100] = new Waypoint(9100, new Vector3(800, 0, 200), state.PlanetCrc, "Combat medic", WaypointColor.Blue, true);
                list[9101] = new Waypoint(9101, new Vector3(-250, 0, 100), state.PlanetCrc, "Player Association Hall", WaypointColor.Green, true);
                list[9102] = new Waypoint(9102, new Vector3(-20, 0, -12), state.PlanetCrc, "Junk Ore", WaypointColor.Blue, true);
            });
            state.SceneObjects.Add(new CreatureSceneObject
            {
                SwgId = 9000,
                Name = "Misty",
                Ham = new HamPool
                {
                    Health = new HamElement(current: 650, maximum: 900, wounds: 100),
                    Action = new HamElement(current: 800, maximum: 1000, wounds: 0),
                    Mind = new HamElement(current: 500, maximum: 700, wounds: 50)
                }
            });
            state.SceneObjects.Add(new CreatureSceneObject
            {
                SwgId = 9001,
                Name = "Target preview",
                Ham = new HamPool
                {
                    Health = new HamElement(current: 750, maximum: 1000, wounds: 100),
                    Action = new HamElement(current: 500, maximum: 1000, wounds: 200),
                    Mind = new HamElement(current: 900, maximum: 1000, wounds: 0)
                }
            });
            scope.Resolve<ClientTargeting>().Select(9001);
            player = state.Commands;
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
