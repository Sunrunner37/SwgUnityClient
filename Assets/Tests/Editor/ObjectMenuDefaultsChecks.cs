using System.Linq;
using System.IO;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;
using NUnit.Framework;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Runtime.Networking;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class ObjectMenuDefaultsChecks
    {
        [Test]
        public void LivingAttackableCreaturesDefaultToAttackButCorpsesDoNot()
        {
            var defaults = GameServicesForTests.CreateMenuDefaults();
            var creature = new CreatureSceneObject { PvpStatus = 1 };
            Assert.That(defaults.TryGetDefaultAction(creature, (ObjectTypeId)0x400, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.Attack));
            Assert.That(action.Id, Is.EqualTo(3), "Core3 COMBAT_ATTACK");
            creature.Posture = CreaturePosture.Dead;
            Assert.That(defaults.TryGetDefaultAction(creature, (ObjectTypeId)0x400, out _), Is.False);
            creature.Posture = CreaturePosture.Upright;
            creature.PvpStatus = 0;
            creature.Options = 8;
            Assert.That(defaults.TryGetDefaultAction(creature, (ObjectTypeId)0x400, out action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.ConverseStart));
        }
        [Test]
        public void SharedWorldMetadataAndCurrentPlayerDetermineMenuEntries()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var state = GameServicesForTests.CreateState(out var player);
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            player.BeginScene(1);
            state.SceneObjects.Add(new CreatureSceneObject { SwgId = 1 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff" });
            var item = new TangibleSceneObject { SwgId = 3, ParentSwgId = 2, TemplatePath = "object/tangible/dice/shared_eqp_six_sided_dice_set.iff" };
            state.SceneObjects.Add(item);
            var defaults = new ObjectMenuDefaults(state, player);
            Assert.That(defaults.Create(item).Any(entry => entry.RadialId == ObjectMenuAction.Destroy.Id), Is.True);
            state.SceneObjects.Reparent(3, default);
            Assert.That(defaults.Create(item).Any(entry => entry.RadialId == ObjectMenuAction.Destroy.Id), Is.False);
            var vehicle = new IntangibleSceneObject { TemplatePath = "object/intangible/vehicle/shared_speederbike_pcd.iff" };
            Assert.That(defaults.TryGetDefaultAction(vehicle, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.VehicleGenerate));
            vehicle.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            Assert.That(defaults.Create(vehicle)[1].RadialId, Is.EqualTo(ObjectMenuAction.VehicleStore.Id));
        }

        [TestCase(ObjectTypeId.CraftingTool)]
        [TestCase(ObjectTypeId.CraftingStation)]
        [TestCase(ObjectTypeId.BankTerminal)]
        public void ClientUseCarriesItsRoutingWithoutChangingTheWireAction(ObjectTypeId objectType)
        {
            Assert.That(GameServicesForTests.CreateMenuDefaults().TryGetDefaultAction(new SceneObject(), objectType, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.ClientUse));
            var entry = action.ToEntry(2);
            Assert.That(entry.RadialId, Is.EqualTo(20));
            Assert.That(entry.Callback, Is.EqualTo(1));
            Assert.That(entry.Text, Is.EqualTo("@ui_radial:item_use"));
            Assert.That(ObjectMenuAction.Use.ToEntry(2).Callback, Is.EqualTo(3));
        }

        [Test]
        public void ActionLookupReusesDefinitionsAndLeavesUnknownServerActionsUnmapped()
        {
            Assert.That(ObjectMenuAction.TryGet(60, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.VehicleGenerate));
            Assert.That(action.Caption, Is.EqualTo("@pet/pet_menu:menu_generate"));
            Assert.That(ObjectMenuAction.TryGet(255, out _), Is.False);
        }

        [TestCase(0x807, 44, 59)] // Creature-handler pets
        [TestCase(0x80b, 44, 59)] // Droids use the same PetControlDevice handler
        [TestCase(0x808, 60, 61)]
        public void ControlDeviceMenuAndDoubleClickUseTheSameReplicatedStatus(int objectType, byte call, byte store)
        {
            var device = new IntangibleSceneObject();
            Assert.That(GameServicesForTests.CreateMenuDefaults().Create(device, (ObjectTypeId)objectType).Select(entry => entry.RadialId), Is.EqualTo(new byte[] { 7, call }));
            Assert.That(GameServicesForTests.CreateMenuDefaults().TryGetDefaultAction(device, (ObjectTypeId)objectType, out var action), Is.True);
            Assert.That(action.Id, Is.EqualTo(call));
            device.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            Assert.That(GameServicesForTests.CreateMenuDefaults().Create(device, (ObjectTypeId)objectType).Select(entry => entry.RadialId), Is.EqualTo(new byte[] { 7, store }));
            GameServicesForTests.CreateMenuDefaults().TryGetDefaultAction(device, (ObjectTypeId)objectType, out action);
            Assert.That(action.Id, Is.EqualTo(store));
            Assert.That(((ObjectTypeId)objectType).IsControlDevice(), Is.True);
        }

        [TestCase(0x200a)]
        [TestCase(0x2018)]
        [TestCase(0x200d)]
        [TestCase(0x201b)]
        [TestCase(0x2019)]
        [TestCase(0x201c)]
        [TestCase(0x400c)]
        [TestCase(0x4011)]
        [TestCase(0x2012)]
        [TestCase(0x2014)]
        [TestCase(0x4003)]
        [TestCase(0x4004)]
        [TestCase(0x400a)]
        [TestCase(0x4012)]
        [TestCase(0x8004)]
        [TestCase(0x20003)]
        [TestCase(0x20004)]
        [TestCase(0x20006)]
        [TestCase(0x800006)]
        [TestCase(0x800008)]
        [TestCase(0x200f)] // Sign
        [TestCase(0x4001)] // Bank root, with server-provided children
        [TestCase(0x8002)] // Survey tool
        [TestCase(0x8003)] // Repair tool
        [TestCase(0x800001)] // Structure deed
        [TestCase(0x800002)] // Installation deed
        public void SuppliesUseOnlyWhereCore3ExpectsIt(int objectType)
        {
            var menu = GameServicesForTests.CreateMenuDefaults().Create(new SceneObject(), (ObjectTypeId)objectType);
            Assert.That(menu.Select(entry => entry.RadialId), Is.EqualTo(new byte[] { 7, 20 }));
            Assert.That(menu[1].Callback, Is.EqualTo(objectType == 0x4001 ? 1 : 3));
        }

        [TestCase(0x80a)] // ShipControlDevice supplies Launch/Land itself
        [TestCase(0x800005)] // Vehicle deed supplies Generate itself
        [TestCase(0x800003)] // Pet deed supplies Tame itself
        [TestCase(0x1003)] // Harvester supplies its own management options
        public void DoesNotDuplicateServerProvidedOptions(int objectType)
        {
            Assert.That(GameServicesForTests.CreateMenuDefaults().Create(new SceneObject(), (ObjectTypeId)objectType).Single().RadialId, Is.EqualTo(7));
            Assert.That(GameServicesForTests.CreateMenuDefaults().TryGetDefaultAction(new SceneObject(), (ObjectTypeId)objectType, out _), Is.False);
        }

        [TestCase("object/tangible/terminal/shared_terminal_travel.iff")]
        [TestCase("object/tangible/item/shared_droid_customization.iff")]
        [TestCase("object/tangible/gambling/slot/shared_standard.iff")]
        [TestCase("object/tangible/skill_buff/shared_skill_buff_unarmed_speed.iff")]
        [TestCase("object/tangible/jedi/shared_jedi_holocron_light.iff")]
        public void GenericTemplatesWithCore3UseComponentsHaveUse(string path)
        {
            Assert.That(GameServicesForTests.CreateMenuDefaults().Create(new SceneObject { TemplatePath = path }, (ObjectTypeId)0).Select(entry => entry.RadialId), Is.EqualTo(new byte[] { 7, 20 }));
        }

        [Test]
        public void LootAppearsOnlyForDeadCreatures()
        {
            var creature = new CreatureSceneObject();
            Assert.That(GameServicesForTests.CreateMenuDefaults().Create(creature, (ObjectTypeId)0x401).Length, Is.EqualTo(1));
            creature.Posture = CreaturePosture.Dead;
            var menu = GameServicesForTests.CreateMenuDefaults().Create(creature, (ObjectTypeId)0x401);
            Assert.That(menu.Select(entry => entry.RadialId), Is.EqualTo(new byte[] { 7, 35, 36 }));
            Assert.That(menu.Select(entry => entry.Index).Distinct().Count(), Is.EqualTo(menu.Length));
        }
    }
}