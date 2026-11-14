using System.Linq;
using Swg.Client.Runtime.Networking.Chat;
using Swg.Client.Runtime.Localization;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using System.Collections;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Packets.Commands;
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
        public bool previewCombatQueue = true;
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
                    interactions = new ClientObjectInteractions(world, request => interactions.Receive(new ObjectMenuResponsePacket(world.PlayerSwgId.Value, request.TargetSwgId.Value, request.Items.Length > 1 ? request.Items : new[]
                    {
                        request.Items[0],
                        new RadialMenuEntry(2, 0, 118, 3, "@player_structure:management"),
                        new RadialMenuEntry(3, 0, 117, 3, "@player_structure:permissions"),
                        new RadialMenuEntry(4, 2, 124, 3, "@player_structure:management_status")
                    }, request.Counter)), swgId => interactions.Receive(new AttributeListPacket(swgId.Value, swgId.Value >= 9300 ? new[] { new AttributeValue("condition", "1000/1000"), new AttributeValue("volume", "1"), new AttributeValue("crafter", "Misty") } : new[] { new AttributeValue("owner", "Trixie") })), packet => ApplyOfflineMenuSelection(world, packet));
                    return interactions;
                }).SingleInstance();
                builder.RegisterInstance(new Admission { Username = "ui-testing", Galaxy = new Galaxy { Name = "Offline" } });
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
                builder.Register(context => new ClientInventory(context.Resolve<WorldState>(), swgId => Debug.Log("Offline inventory opened: " + swgId))).SingleInstance();
                builder.RegisterInstance(new Action<CommandQueueEnqueuePacket>(packet => Debug.Log("Offline queue enqueue: " + packet.CommandCrc)));
                builder.RegisterInstance(new Action<CommandQueueDequeuePacket>(packet => Debug.Log("Offline queue cancel: " + packet.ActionCounter)));
                builder.RegisterType<ClientCommands>().SingleInstance();
                builder.Register(context => new ClientChat(null, context.Resolve<WorldState>(), context.Resolve<StringTableLocalizer>(), context.Resolve<ProseFormatter>(), context.Resolve<ClientCommands>())).SingleInstance();
                builder.Register(context =>
                {
                    var commands = context.Resolve<ClientCommands>();
                    return new Action<string, string, SwgId>((name, arguments, target) => commands.Enqueue(name, arguments, target));
                }).SingleInstance();
                builder.RegisterType<ClientDatapad>().AsSelf().SingleInstance();
                builder.RegisterType<ClientCombatQueue>().SingleInstance();
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.RegisterType<ClientFlyText>().SingleInstance();
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
                TemplatePath = "object/creature/player/shared_human_female.iff",
                CashCredits = 125,
                BankCredits = 11410,
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
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 9200, ParentSwgId = 9000, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff", Name = "Inventory" });
            string[] templates = {
                "object/resource_container/shared_resource_container_inorganic_minerals.iff",
                "object/resource_container/shared_resource_container_inorganic_chemicals.iff",
                "object/tangible/survey_tool/shared_survey_tool_mineral.iff",
                "object/tangible/crafting/station/shared_structure_tool.iff",
                "object/tangible/wearables/armor/composite/shared_armor_composite_boots.iff",
                "object/tangible/wearables/armor/composite/shared_armor_composite_chest_plate.iff"
            };
            string[] itemNames = { "Aluminum", "Copper", "Chemical Survey Device", "Structure and Furniture Crafting Tool", "Composite Armor Boots", "Composite Armor Chest Plate" };
            for (int i = 0; i < 24; i++)
                state.SceneObjects.Add(new TangibleSceneObject { SwgId = (ulong)(9300 + i), ParentSwgId = 9200, TemplatePath = templates[i % templates.Length], Name = itemNames[i % templates.Length], Quantity = i % templates.Length < 2 ? 41000 + i * 1000 : 1 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 9800, ParentSwgId = 9000, TemplatePath = "object/tangible/datapad/shared_character_datapad.iff", Name = "Datapad" });
            state.SceneObjects.Add(new IntangibleSceneObject { SwgId = 9801, ParentSwgId = 9800, TemplatePath = "object/intangible/pet/shared_r2.iff", Name = "A helper R2 unit" });
            state.SceneObjects.Add(new IntangibleSceneObject { SwgId = 9802, ParentSwgId = 9800, TemplatePath = "object/intangible/pet/shared_r2_crafted.iff", Name = "R2 crafting assistant" });
            state.SceneObjects.Add(new IntangibleSceneObject { SwgId = 9803, ParentSwgId = 9800, TemplatePath = "object/intangible/vehicle/shared_landspeeder_x31_pcd.iff", Name = "Landspeeder" });
            state.UpdateDraftSchematics(list => list.AddRange(new[]
            {
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/weapon/shared_rifle_t21.iff"),
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/item/shared_item_recycler_flora.iff"),
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/weapon/shared_rifle_flame_thrower.iff")
            }));
            foreach (uint crc in state.DraftSchematics)
                state.UpdateDraftDetails(crc, new DraftSchematicDetails
                {
                    Complexity = 7,
                    Size = 4,
                    Ingredients = new[] { new DraftSchematicDetails.Ingredient("Metal", "metal", 8, false) },
                    Experiments = SharedDraftSchematicTemplate.Attributes(GameServices.Resolve<TreRuntimeAssets>().Loader.Read(state.Templates.Resolve(crc)))
                        .Select(attribute => string.IsNullOrEmpty(attribute.Experiment) ? Array.Empty<DraftSchematicDetails.ExperimentWeight>() : new[]
                        {
                            new DraftSchematicDetails.ExperimentWeight("Overall quality", 2),
                            new DraftSchematicDetails.ExperimentWeight("Unit toughness", 1)
                        }).ToArray()
                });
            var preview = FindAnyObjectByType<InventoryWindow>(FindObjectsInactive.Include).characterPreview;
            scope.Resolve<WorldRenderingContext>().Initialize(preview.worldShader, preview.characterShader, preview.particleShader, null, null, this);
            scope.Resolve<InventoryPreviewCache>();
            scope.Resolve<ClientTargeting>().Select(9001);
            player = state.Commands;
            RefreshAbilities();
        }


        static void ApplyOfflineMenuSelection(WorldState world, ObjectMenuSelectPacket packet)
        {
            Debug.Log("Offline radial: " + packet.RadialId + " on " + packet.TargetSwgId);
            bool activate = packet.RadialId == ObjectMenuDefaults.VehicleGenerateAction || packet.RadialId == ObjectMenuDefaults.PetCallAction;
            bool store = packet.RadialId == ObjectMenuDefaults.VehicleStoreAction || packet.RadialId == ObjectMenuDefaults.PetStoreAction;
            if (!activate && !store) return;
            if (!world.SceneObjects.TryGet<IntangibleSceneObject>(packet.TargetSwgId, out var device) || !ObjectMenuDefaults.IsControlDevice(world.Templates.GameObjectType(device.TemplatePath))) return;
            var update = new IntangibleSceneObjectUpdate { Status = activate ? 1 : 0 };
            device.Apply(update);
            world.SceneObjects.NotifyChanged(device, update.Changes);
        }

        public void ShowSampleMenu() => FindAnyObjectByType<RadialContextMenu>().Open(9002, new Vector2(Screen.width * .5f, Screen.height * .5f));

        IEnumerator Start()
        {
            yield return FindAnyObjectByType<UiAssetPreloader>().Prewarm();
            if (!previewCombatQueue)
                yield break;
            var catalog = scope.Resolve<CommandCatalog>();
            if (!catalog.Loading.IsCompleted)
                yield return new WaitUntil(() => catalog.Loading.IsCompleted);
            if (catalog.Loading.IsFaulted)
            {
                Debug.LogException(catalog.Loading.Exception.GetBaseException(), this);
                yield break;
            }
            var state = scope.Resolve<WorldState>();
            state.SceneObjects.TryGet<CreatureSceneObject>(state.PlayerSwgId, out var creature);
            creature.States |= CreatureState.Combat;
            state.SceneObjects.NotifyChanged(creature, SceneObjectChange.State);
            var commands = scope.Resolve<ClientCommands>();
            commands.Enqueue("intimidate1", targetSwgId: 9001);
            commands.Enqueue("attack", targetSwgId: 9001);
            commands.Enqueue("attack", targetSwgId: 9001);
        }

        public void RefreshAbilities() => player.Apply(list =>
        {
            list.Clear();
            list.AddRange(sampleAbilities);
        });

        void OnDestroy() => GameServices.EndGame(scope);
    }
}
