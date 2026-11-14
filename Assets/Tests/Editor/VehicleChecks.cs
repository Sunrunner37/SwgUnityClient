using System.IO;
using System.Linq;
using UnityEngine;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Configuration;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
namespace Swg.Client.Tests.Editor
{
    public sealed class VehicleChecks
    {
        TreAssetLoader loader;
        ObjectTemplateCatalog templates;
        [OneTimeSetUp]
        public void LoadTemplates()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            templates = new ObjectTemplateCatalog(loader);
        }
        [OneTimeTearDown] public void DisposeTemplates() => loader.Dispose();
        SynchronizationContext previous;
        [SetUp] public void SetUp() { previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(new SynchronizationContext()); }
        [TearDown] public void TearDown() => SynchronizationContext.SetSynchronizationContext(previous);

        [TestCase("object/intangible/vehicle/shared_landspeeder_x31_pcd.iff", 0x808)]
        [TestCase("object/intangible/pet/shared_r2.iff", 0x80b)]
        [TestCase("object/intangible/pet/shared_bantha_hue.iff", 0x807)]
        [TestCase("object/tangible/terminal/shared_terminal_bank.iff", 0x4001)]
        [Category("NativeAssets")]
        public void DefaultClassificationUsesInheritedNativeMetadata(string path, int expected)
        {
            Assert.That(templates.GameObjectType(path), Is.EqualTo(expected));
        }

        [Test]
        public void DroidDoubleClickAndRadialUsePetActionsAndWaitForTheServer()
        {
            var state = new WorldState(new SceneObjectRegistry()) { Templates = templates };
            var droid = (IntangibleSceneObject)SceneObjectFactory.Create(10, "object/intangible/pet/shared_r2.iff");
            state.SceneObjects.Add(droid);
            ObjectMenuSelectPacket sent = default;
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, packet => sent = packet, (name, arguments, target) => { });
            interactions.RequestMenu(10);
            Assert.That(interactions.Menu[1].Text, Is.EqualTo("@pet/pet_menu:menu_call"));
            interactions.ActivateDefault(10);
            Assert.That(sent.RadialId, Is.EqualTo(44));
            Assert.That(droid.IsActive, Is.False);
            droid.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            Assert.That(interactions.IsActiveControlDevice(10), Is.True);
            interactions.RequestMenu(10);
            Assert.That(interactions.Menu[1].RadialId, Is.EqualTo(59));
            interactions.ActivateDefault(10);
            Assert.That(sent.RadialId, Is.EqualTo(interactions.Menu[1].RadialId));
        }

        [Test, Category("NativeAssets")]
        public void LandspeederIncludesTheNativeTableSelectedBody()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            Assert.That(VehicleBodyAppearance.TryGet(loader, "appearance/pv_landspeeder_luke.sat", out var body), Is.True);
            Assert.That(body, Is.EqualTo("appearance/landspeeder_luke.apt"));
            var appearance = SkeletalAppearanceLoader.LoadDetailed(loader, "object/mobile/vehicle/shared_landspeeder_x31.iff");
            Assert.That(appearance.Parts.Sum(part => part.Vertices.Length), Is.GreaterThan(1000));
            Assert.That(appearance.Parts.SelectMany(part => part.Vertices).Max(vertex => vertex.z) - appearance.Parts.SelectMany(part => part.Vertices).Min(vertex => vertex.z), Is.GreaterThan(5));
            Assert.That(VehicleBodyAppearance.TryGet(loader, "appearance/hum_f.sat", out _), Is.False);
        }
        [Test]
        public void MenuSelectionMatchesCore3ObjectIdAndRadialIdPayload()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            new ObjectMenuSelectPacket(123, ObjectMenuDefaults.VehicleGenerateAction).WriteTo(writer);
            var reader = new ByteReader(stream.ToArray());
            Assert.That(reader.U16(), Is.EqualTo(3));
            Assert.That(reader.U32(), Is.EqualTo(0x7ca18726u));
            Assert.That(reader.U64(), Is.EqualTo(123));
            Assert.That(reader.Byte(), Is.EqualTo(60));
            Assert.That(reader.Remaining, Is.Zero);
            Assert.That(ObjectMenuSelectPacket.ReadFrom(new ByteReader(stream.ToArray())).TargetSwgId, Is.EqualTo((SwgId)123));
        }
        [Test]
        public void IntangibleBaselineAndDeltaRetainServerVehicleStatus()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(1f);
            PacketFieldCodec.Ascii(writer, "vehicle_name"); writer.Write(0u); PacketFieldCodec.Ascii(writer, "speederbike");
            PacketFieldCodec.Unicode(writer, "My bike"); writer.Write(0u); writer.Write(1u);
            var device = (IntangibleSceneObject)SceneObjectFactory.Create(10, "object/intangible/vehicle/shared_speederbike_pcd.iff");
            device.Apply(IntangibleView3.ReadBaseline(new ByteReader(stream.ToArray())));
            Assert.That(device.Name, Is.EqualTo("My bike"));
            Assert.That(device.IsActive, Is.True);
            var update = new IntangibleSceneObjectUpdate();
            Assert.That(IntangibleView3.ReadDeltaField(4, new ByteReader(new byte[4]), update), Is.True);
            device.Apply(update);
            Assert.That(device.IsActive, Is.False);
            Assert.That(update.Changes.HasFlag(SceneObjectChange.Inventory), Is.True);
            device.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            var copy = new IntangibleSceneObject(); device.CopyTo(copy);
            Assert.That(copy.IsActive, Is.True);
        }
        [Test]
        public void DoubleClickWaitsForReplicatedStatusAndUsesSameStoreActionAsWorldMenu()
        {
            var state = new WorldState(new SceneObjectRegistry()) { Templates = templates };
            var device = (IntangibleSceneObject)SceneObjectFactory.Create(10, "object/intangible/vehicle/shared_speederbike_pcd.iff");
            state.SceneObjects.Add(device);
            state.SceneObjects.Add(SceneObjectFactory.Create(11, "object/mobile/vehicle/shared_speederbike.iff"));
            ObjectMenuSelectPacket sent = default;
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, packet => sent = packet, (name, arguments, target) => { });
            interactions.ActivateDefault(10);
            Assert.That(sent.RadialId, Is.EqualTo(60));
            Assert.That(device.IsActive, Is.False);
            device.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            interactions.ActivateDefault(10);
            Assert.That(sent.RadialId, Is.EqualTo(61));
            Assert.That(device.IsActive, Is.True);
            interactions.SelectMenuAction(11, ObjectMenuDefaults.VehicleStoreAction);
            Assert.That(sent.TargetSwgId, Is.EqualTo((SwgId)11));
            Assert.That(sent.RadialId, Is.EqualTo(61));
        }
        [Test]
        public void DatapadRadialDefaultsOfferGenerateOrStoreFromCurrentServerState()
        {
            var state = new WorldState(new SceneObjectRegistry()) { Templates = templates };
            var device = (IntangibleSceneObject)SceneObjectFactory.Create(10, "object/intangible/vehicle/shared_speederbike_pcd.iff");
            state.SceneObjects.Add(device);
            ObjectMenuRequestPacket request = default;
            using var interactions = new ClientObjectInteractions(state, packet => request = packet, _ => { }, _ => { }, (name, arguments, target) => { });
            interactions.RequestMenu(10);
            Assert.That(request.Items[0].RadialId, Is.EqualTo(ObjectMenuDefaults.ExamineAction));
            Assert.That(request.Items[1].RadialId, Is.EqualTo(ObjectMenuDefaults.VehicleGenerateAction));
            device.Apply(new IntangibleSceneObjectUpdate { Status = 1 });
            interactions.RequestMenu(10);
            Assert.That(request.Items[1].RadialId, Is.EqualTo(ObjectMenuDefaults.VehicleStoreAction));
        }
    }
}
