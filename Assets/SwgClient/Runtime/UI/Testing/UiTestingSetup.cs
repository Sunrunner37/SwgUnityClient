using Swg.Client.Core.Networking.Game.Packets.Trade;
using Swg.Client.Runtime.Assets.Particles;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Packets.Player;
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
using Swg.Client.Core.Networking.Game.Packets.Mission;
using Swg.Client.Core.Networking.Game.Packets.Crafting;
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
        public CombatTestingSetup combatTesting;
        public string[] sampleAbilities = Array.Empty<string>();
        CurrentPlayer currentPlayer;
        ILifetimeScope scope;

        void Awake()
        {
            GameServices.Disconnect();
            scope = GameServices.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<SceneObjectRegistry>().SingleInstance();
                builder.RegisterType<CurrentPlayer>().SingleInstance();
                builder.RegisterInstance(new Action<TradeRequestPacket>(packet => Debug.Log("Offline trade request: " + packet.TargetSwgId)));
                builder.RegisterType<CraftingProgress>().SingleInstance();
                builder.RegisterType<WorldState>().SingleInstance();
                builder.RegisterType<ObjectMenuDefaults>().SingleInstance();
                builder.RegisterInstance(new Action<TargetUpdatePacket>(packet => Debug.Log("Offline target: " + packet.TargetSwgId)));
                builder.RegisterType<ClientTargeting>().AsSelf().SingleInstance().AutoActivate();
                builder.Register(context =>
                {
                    var world = context.Resolve<WorldState>();
                    ClientObjectInteractions interactions = null;
                    interactions = new ClientObjectInteractions(world, request => interactions.Receive(new ObjectMenuResponsePacket(currentPlayer.SwgId.Value, request.TargetSwgId.Value, request.Items.Length > 1 ? request.Items : new[]
                    {
                        request.Items[0],
                        new RadialMenuEntry(2, 0, 118, 3, "@player_structure:management"),
                        new RadialMenuEntry(3, 0, 117, 3, "@player_structure:permissions"),
                        new RadialMenuEntry(4, 2, 124, 3, "@player_structure:management_status")
                    }, request.Counter)), swgId => interactions.Receive(new AttributeListPacket(swgId.Value, swgId.Value >= 9300 ? new[] { new AttributeValue("condition", "1000/1000"), new AttributeValue("volume", "1"), new AttributeValue("crafter", "Misty") } : new[] { new AttributeValue("owner", "Trixie") })), packet => ApplyOfflineMenuSelection(world, packet), context.Resolve<Action<string, string, SwgId>>(), context.Resolve<CurrentPlayer>(), context.Resolve<ObjectMenuDefaults>());
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
                builder.Register(context => new ClientInventory(context.Resolve<WorldState>(), swgId => Debug.Log("Offline inventory opened: " + swgId), context.Resolve<CurrentPlayer>())).SingleInstance();
                builder.RegisterInstance(new Action<CommandQueueEnqueuePacket>(packet => Debug.Log("Offline queue enqueue: " + packet.CommandCrc)));
                builder.RegisterInstance(new Action<CommandQueueDequeuePacket>(packet => Debug.Log("Offline queue cancel: " + packet.ActionCounter)));
                builder.RegisterType<ClientCommands>().SingleInstance();
                builder.Register(context => new ClientChat(null, context.Resolve<WorldState>(), context.Resolve<StringTableLocalizer>(), context.Resolve<ProseFormatter>(), context.Resolve<ClientCommands>(), context.Resolve<CurrentPlayer>())).SingleInstance();
                builder.Register(context =>
                {
                    var commands = context.Resolve<ClientCommands>();
                    var world = context.Resolve<WorldState>();
                    return new Action<string, string, SwgId>((name, arguments, target) =>
                    {
                        commands.Enqueue(name, arguments, target);
                        if (name == "requestStatMigrationData")
                            currentPlayer.CharacterSheet.Update(data => data.HasMigrationTargets = true, CharacterSheetChange.Migration);
                    });
                }).SingleInstance();
                builder.RegisterInstance(new Action<FactionRequestPacket>(packet => Debug.Log("Offline faction standings requested")));
                builder.RegisterType<ClientCharacterSheet>().SingleInstance();
                builder.RegisterType<ClientConversation>().SingleInstance();
                builder.RegisterInstance(new Action<SuiEventNotificationPacket>(packet => Debug.Log("Offline dialog response: " + packet.PageId)));
                builder.RegisterType<ClientVehicleRepairs>().SingleInstance();
                builder.RegisterType<ClientMissions>().AsSelf().SingleInstance().AutoActivate();
                builder.Register(context => { var world = context.Resolve<WorldState>(); return new Action<MissionListRequestPacket>(packet => StartCoroutine(DownloadMissions(world, packet))); }).SingleInstance();
                builder.Register(context => { var world = context.Resolve<WorldState>(); return new Action<MissionAcceptPacket>(packet => { world.SceneObjects.Reparent(packet.MissionSwgId, 9800); }); }).SingleInstance();
                builder.Register(context => { var world = context.Resolve<WorldState>(); return new Action<MissionAbortPacket>(packet => world.SceneObjects.Remove(packet.MissionSwgId)); }).SingleInstance();
                builder.RegisterType<ClientCrafting>().AsSelf().SingleInstance();
                builder.RegisterInstance(new Action<CraftingRemoveIngredientPacket>(packet => Debug.Log("Offline crafting remove: " + packet.Slot)));
                builder.RegisterInstance(new Action<CraftingAddIngredientPacket>(packet => Debug.Log("Offline crafting: " + packet)));
                builder.RegisterInstance(new Action<CraftingExperimentPacket>(packet => Debug.Log("Offline crafting: " + packet)));
                builder.RegisterInstance(new Action<CraftingCustomizationPacket>(packet => Debug.Log("Offline crafting: " + packet)));
                builder.RegisterType<ClientSkills>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<DraftSchematicCatalog>().AsSelf().SingleInstance();
                builder.RegisterType<ClientDatapad>().AsSelf().SingleInstance();
                builder.RegisterType<ClientCombat>().AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<ClientEntertainment>().AsSelf().SingleInstance().AutoActivate();
                builder.Register(context =>
                {
                    var effects = new GameObject("Client effects").AddComponent<ClientEffects>();
                    effects.Initialize(context.Resolve<TreRuntimeAssets>(), context.Resolve<WorldState>(), context.Resolve<ClientCombat>());
                    UnityEngine.Object.DontDestroyOnLoad(effects.gameObject);
                    return effects;
                }).AsSelf().SingleInstance().AutoActivate();
                builder.RegisterType<ClientCombatQueue>().SingleInstance();
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
                builder.RegisterType<ClientFlyText>().SingleInstance();
                builder.RegisterType<OfflineCommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
                if (combatTesting != null) combatTesting.Register(builder);
            });
            currentPlayer = scope.Resolve<CurrentPlayer>();
            var state = scope.Resolve<WorldState>();
            currentPlayer.BeginScene(9000);
            state.BeginScene(Vector3.zero, "terrain/tatooine.trn");
            state.Templates = new ObjectTemplateCatalog(scope.Resolve<TreRuntimeAssets>().Loader);
            currentPlayer.UpdateWaypoints(list =>
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
            state.SceneObjects.TryGet<CreatureSceneObject>(9000, out var groupPlayer);
            groupPlayer.GroupSwgId = 9900;
            var sampleGroup = new GroupSceneObject { SwgId = 9900 };
            sampleGroup.Apply(new GroupSceneObjectUpdate { Members = list =>
            {
                list.Add(new GroupMember(9000, "Misty"));
                list.Add(new GroupMember(9901, "Unavailable group member"));
            } });
            state.SceneObjects.Add(sampleGroup);
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
            state.SceneObjects.Add(new SceneObject { SwgId = 9850, ParentSwgId = 9000, TemplatePath = "object/tangible/mission_bag/shared_mission_bag.iff" });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 9851, Name = "Mission terminal", TemplatePath = "object/tangible/terminal/shared_terminal_mission.iff" });
            string[] authors = { "Wedofee Agame", "Waclome Hesri", "Trisloayo Liani", "Taseke Tye", "Fitoe Lightingdust", "Fifa Kriosoto" };
            string[] titles = { "Overland Migration Menaces Mandracor", "Professor Seeks Specimen", "Destroy one of Valarian's strongholds", "Take out criminal hospital", "Dispatch Breeding Colonies", "Shistavanen Requires Carcass" };
            string target = "object/tangible/lair/ronto/shared_lair_ronto_desert.iff";
            for (int i = 0; i < 12; i++)
                state.SceneObjects.Add(new MissionSceneObject
                {
                    SwgId = (ulong)(9860 + i), ParentSwgId = 9850, TemplatePath = "object/mission/shared_mission_object.iff",
                    Title = titles[i % 6], Creator = authors[i % 6], Reward = 12000 - i * 233,
                    Difficulty = 56, StartPosition = new Vector3(i * 130, 0, 866 - i * 28),
                    StartPlanetCrc = state.PlanetCrc, MissionTypeCrc = i < 6 ? 0x74ef9be3u : 0xe5c27ec6u,
                    TargetName = "a ronto nest", TargetTemplateCrc = ObjectTemplateNames.TemplateCrc(target),
                    Description = "A dangerous breeding colony has appeared near our settlement. We are offering a substantial reward to anyone who can destroy the nest before it threatens the local population.",
                    Waypoint = new Waypoint((ulong)(9880 + i), new Vector3(i * 130, 0, 866 - i * 28), state.PlanetCrc, titles[i % 6], WaypointColor.Orange, true)
                });
            currentPlayer.UpdateDraftSchematics(list => list.AddRange(new[]
            {
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/weapon/shared_rifle_t21.iff"),
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/item/shared_item_recycler_flora.iff"),
                ObjectTemplateNames.TemplateCrc("object/draft_schematic/weapon/shared_rifle_flame_thrower.iff")
            }));
            foreach (uint crc in currentPlayer.DraftSchematics)
                currentPlayer.UpdateDraftDetails(crc, new DraftSchematicDetails
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
            state.SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var character);
            character.BattleFatigue = 100;
            character.Ham = new HamPool
            {
                Health = new HamElement(650, 900, 100, 700), Strength = new HamElement(398, 400, 2, 400), Constitution = new HamElement(398, 400, 2, 400),
                Action = new HamElement(700, 900, 100, 900), Quickness = new HamElement(499, 500, 1, 500), Stamina = new HamElement(399, 400, 1, 400),
                Mind = new HamElement(900, 1000, 100, 1000), Focus = new HamElement(500, 500, 0, 500), Willpower = new HamElement(600, 600, 0, 600)
            };
            currentPlayer.CharacterSheet.Update(data =>
            {
                data.Food = 65; data.FoodMaximum = 100; data.Drink = 35; data.DrinkMaximum = 100;
                data.LotsRemaining = 200; data.BadgeMasks = new uint[] { 3, 0, 0, 0, 0 };
                data.Biography = "An explorer finding her way across the galaxy.";
                data.MigrationTargets = HamAttributes.FromArray(new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 });
                data.MigrationBudget = data.MigrationTargets.Total; data.HasMigrationTargets = true;
            });
            var preview = FindAnyObjectByType<InventoryWindow>(FindObjectsInactive.Include).characterPreview;
            scope.Resolve<WorldRenderingContext>().Initialize(preview.worldShader, preview.characterShader, preview.particleShader, null, null, this);
            scope.Resolve<InventoryPreviewCache>();
            scope.Resolve<ClientTargeting>().Select(9001);
            RefreshAbilities();
        }


        IEnumerator DownloadMissions(WorldState state, MissionListRequestPacket packet)
        {
            yield return new WaitForSecondsRealtime(.8f);
            foreach (var mission in state.SceneObjects.ChildrenOf(9850).OfType<MissionSceneObject>().ToArray())
            {
                mission.RefreshCounter = packet.Sequence;
                state.SceneObjects.NotifyChanged(mission, SceneObjectChange.Inventory);
            }
        }

        [ContextMenu("Preview mission terminal")]
        public void ShowSampleMissions() => scope.Resolve<ClientMissions>().Open(9851);

        void ApplyOfflineMenuSelection(WorldState world, ObjectMenuSelectPacket packet)
        {
            Debug.Log("Offline radial: " + packet.RadialId + " on " + packet.TargetSwgId);
            bool activate = packet.RadialId == ObjectMenuAction.VehicleGenerate.Id || packet.RadialId == ObjectMenuAction.PetCall.Id;
            bool store = packet.RadialId == ObjectMenuAction.VehicleStore.Id || packet.RadialId == ObjectMenuAction.PetStore.Id;
            if (!activate && !store) return;
            if (!world.SceneObjects.TryGet<IntangibleSceneObject>(packet.TargetSwgId, out var device) || !((ObjectTypeId)world.Templates.GameObjectType(device.TemplatePath)).IsControlDevice()) return;
            var update = new IntangibleSceneObjectUpdate { Status = activate ? 1 : 0 };
            device.Apply(update);
            world.SceneObjects.NotifyChanged(device, update.Changes);
        }

        public void ShowSampleMenu() => FindAnyObjectByType<RadialContextMenu>().Open(9002, new Vector2(Screen.width * .5f, Screen.height * .5f));

        IEnumerator Start()
        {
            yield return FindAnyObjectByType<UiAssetPreloader>().Prewarm();
            var skills = scope.Resolve<ClientSkills>();
            yield return new WaitUntil(() => skills.Loading.IsCompleted);
            if (skills.Loading.IsFaulted) { Debug.LogException(skills.Loading.Exception.GetBaseException(), this); yield break; }
            var previewState = scope.Resolve<WorldState>();
            previewState.SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var previewPlayer);
            previewPlayer.Skills.AddRange(new[] { "crafting_artisan_novice", "social_entertainer_novice", "social_musician_novice" });
            var artisan = skills.Catalog.Skills["crafting_artisan"];
            previewPlayer.Skills.AddRange(skills.Catalog.Tree(artisan).Skills.Where(skill => skill.Name.Contains("_survey_")).Select(skill => skill.Name));
            foreach (string name in previewPlayer.Skills)
                foreach (var modifier in skills.Catalog.Skills[name].Modifiers)
                    previewPlayer.SkillModifiers[modifier.Key] = previewPlayer.SkillModifiers.TryGetValue(modifier.Key, out int value) ? value + modifier.Value : modifier.Value;
            currentPlayer.UpdateExperience(values =>
            {
                values["crafting_general"] = 1300;
                values["combat_general"] = 806;
                values["music"] = 970;
                values["resource_harvesting_inorganic"] = 20000;
            });
            currentPlayer.Commands.Apply(values => values.AddRange(new[] { "cert_pistol_cdef", "cert_rifle_cdef", "cert_carbine_cdef" }));
            previewState.SceneObjects.NotifyChanged(previewPlayer, SceneObjectChange.Skills);

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
            state.SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var creature);
            creature.States |= CreatureState.Combat;
            state.SceneObjects.NotifyChanged(creature, SceneObjectChange.State);
            var commands = scope.Resolve<ClientCommands>();
            commands.Enqueue("intimidate1", targetSwgId: 9001);
            commands.Enqueue("attack", targetSwgId: 9001);
            commands.Enqueue("attack", targetSwgId: 9001);
        }

        public void RefreshAbilities() => currentPlayer.Commands.Apply(list =>
        {
            list.Clear();
            list.AddRange(sampleAbilities);
        });

        void OnDestroy() => GameServices.EndGame(scope);
    }
}
