using Swg.Client.Runtime.World.Loading;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
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
                builder.Register(context =>
                {
                    var world = context.Resolve<WorldState>();
                    ClientObjectInteractions interactions = null;
                    interactions = new ClientObjectInteractions(world, request => interactions.Receive(new ObjectMenuResponsePacket(world.PlayerSwgId.Value, request.TargetSwgId.Value, new[]
                    {
                        request.Items[0],
                        new RadialMenuEntry(2, 0, 118, 3, "@player_structure:management"),
                        new RadialMenuEntry(3, 0, 117, 3, "@player_structure:permissions"),
                        new RadialMenuEntry(4, 2, 124, 3, "@player_structure:management_status")
                    }, request.Counter)), swgId => interactions.Receive(new AttributeListPacket(swgId.Value, new[] { new AttributeValue("owner", "Trixie") })));
                    return interactions;
                }).SingleInstance();
                builder.RegisterInstance(new Admission { Username = "ui-testing", Galaxy = new Galaxy { Name = "Offline" } });
                builder.RegisterType<AppearanceCache>().SingleInstance();
                builder.RegisterType<WorldContentLoader>().SingleInstance();
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.RegisterType<OfflineCommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
            });
            var state = scope.Resolve<WorldState>();
            state.BeginScene(9000, Vector3.zero, "terrain/tatooine.trn");
            state.Templates = new ObjectTemplateCatalog(scope.Resolve<TreRuntimeAssets>().Loader);
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
            state.SceneObjects.Add(new SpatialSceneObject
            {
                SwgId = 9002,
                Name = "Mineral Mining Installation",
                TemplatePath = "object/installation/mining_ore/shared_mining_ore_harvester_style_2.iff"
            });
            scope.Resolve<ClientTargeting>().Select(9001);
            player = state.Commands;
            RefreshAbilities();
        }

        public void ShowSampleMenu() => FindAnyObjectByType<RadialContextMenu>().Open(9002, new Vector2(Screen.width * .5f, Screen.height * .5f));

        public void RefreshAbilities() => player.Apply(list =>
        {
            list.Clear();
            list.AddRange(sampleAbilities);
        });

        void OnDestroy() => GameServices.EndGame(scope);
    }
}
