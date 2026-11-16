using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Assets.Appearances;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.World.SceneObjects;

namespace Swg.Client.Tests.Editor
{
    public sealed class RidingChecks
    {
        [Test, Category("NativeAssets")]
        public void SpawnedVehiclesUseRenderedGroundWithoutChangingServerCoordinates()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var registry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(registry);
            var state = new WorldState(registry);
            var serverPosition = new Vector3(187.23f, 3.58f, -5028.83f);
            currentPlayer.BeginScene(1);
            state.BeginScene(serverPosition, "terrain/corellia.trn");
            state.Terrain = WorldTerrain.Load(loader, state.TerrainPath);
            var vehicle = new CreatureSceneObject { SwgId = 2, Position = serverPosition };
            registry.Add(vehicle);
            float ground = state.Terrain.SampleHeight(serverPosition.x, serverPosition.z);
            Assert.That(ground, Is.GreaterThan(serverPosition.y + 10), "Reproduces Misty's server/client ground discrepancy.");
            Assert.That(SceneObjectMovement.GetLocalPose(state, vehicle).position.y, Is.EqualTo(ground - state.RenderOrigin.y));
            Assert.That(vehicle.Position, Is.EqualTo(serverPosition), "Grounding must not rewrite replicated coordinates.");

            vehicle.Position.y = ground + 5;
            Assert.That(SceneObjectMovement.GetLocalPose(state, vehicle).position.y, Is.EqualTo(ground + 5 - state.RenderOrigin.y), "Elevated objects retain their altitude.");
            vehicle.Position = serverPosition;
            var building = new TangibleSceneObject { SwgId = 3, Position = serverPosition };
            registry.Add(building);
            Assert.That(SceneObjectMovement.GetLocalPose(state, building).position.y, Is.Zero, "Structures retain their authored placement.");
            registry.Reparent(vehicle.SwgId, building.SwgId);
            Assert.That(SceneObjectMovement.GetLocalPose(state, vehicle).position.y, Is.EqualTo(serverPosition.y), "Contained objects retain their parent-relative placement.");
        }
        [TestCase("object/mobile/vehicle/shared_landspeeder_x31.iff", "vehicle_landspeeder")]
        [TestCase("object/mobile/vehicle/shared_speederbike_swoop.iff", "vehicle_swoop_bike")]
        [Category("NativeAssets")]
        public void VehiclesSupplyTheirNativeDriverSeatAndDistinctRidingPose(string path, string pose)
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var vehicle = SkeletalAppearanceLoader.LoadDetailed(loader, path);
            Assert.That(vehicle.RiderSeat.HasValue, Is.True);
            Assert.That(vehicle.RiderSeat.Value.PoseName, Is.EqualTo(pose));
            Assert.That(vehicle.RiderSeat.Value.Transform.GetColumn(3).y, Is.GreaterThan(.2f));
            var skeleton = SkeletonData.Load(loader, "appearance/hum_f.sat");
            Assert.That(skeleton.RidingPoses.ContainsKey(pose), Is.True);
            var speeder = skeleton.RidingPoses["vehicle_landspeeder"];
            var swoop = skeleton.RidingPoses["vehicle_swoop_bike"];
            float difference = skeleton.Joints.Max(joint => Quaternion.Angle(speeder.Sample(joint.Name, 0).Rotation, swoop.Sample(joint.Name, 0).Rotation));
            Assert.That(difference, Is.GreaterThan(10));
            var root = new GameObject("Rider pose check");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                animation.SetRidingPose(pose);
                animation.Advance(20, .25f);
                int rootIndex = System.Array.FindIndex(skeleton.Joints, joint => joint.Name == "root");
                Assert.That(bones[rootIndex].localPosition, Is.EqualTo(Vector3.zero), "The native root modifier anchors the pelvis to the seat.");
                Assert.That(animation.LocomotionBlend, Is.Zero, "Vehicle travel must not blend in running.");
                animation.SetRidingPose(null);
                Assert.That(bones[rootIndex].localPosition.y, Is.GreaterThan(.5f), "Dismount restores the standing skeleton root.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("object/mobile/vehicle/shared_landspeeder_x31.iff")]
        [TestCase("object/mobile/vehicle/shared_speederbike_swoop.iff")]
        [Category("NativeAssets")]
        public void VehicleDoubleClickUsesMountAndDismountCommands(string template)
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            try
            {
                var registry = new SceneObjectRegistry();
                var currentPlayer = new CurrentPlayer(registry);
                var state = new WorldState(registry) { Templates = new ObjectTemplateCatalog(loader) };
                currentPlayer.BeginScene(1);
                var player = new CreatureSceneObject { SwgId = 1 };
                registry.Add(player);
                registry.Add(new CreatureSceneObject { SwgId = 2, TemplatePath = template });
                string command = null;
                SwgId target = default;
                using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, _ => Assert.Fail("Vehicle default must not send a radial selection packet"),
                    (name, arguments, swgId) => { command = name; target = swgId; }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                interactions.ActivateDefault(2);
                Assert.That(command, Is.EqualTo("mount"));
                Assert.That(target, Is.EqualTo((SwgId)2));
                Assert.That(player.Mount, Is.Null);
                registry.Reparent(1, 2); // Server confirms mounting.
                interactions.ActivateDefault(2);
                Assert.That(command, Is.EqualTo("dismount"));
                Assert.That(target, Is.EqualTo((SwgId)2));
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void EnterExitQueuesCommandsWithoutOptimisticallyChangingContainment()
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            try
            {
                var registry = new SceneObjectRegistry();
                var currentPlayer = new CurrentPlayer(registry);
            var state = new WorldState(registry);
                currentPlayer.BeginScene(1);
                state.BeginScene(Vector3.zero, "terrain/test.trn");
                var player = new CreatureSceneObject { SwgId = 1 };
                var vehicle = new CreatureSceneObject { SwgId = 2 };
                registry.Add(player); registry.Add(vehicle);
                string command = null;
                SwgId target = default;
                int callbacks = 0;
                using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, _ => callbacks++, (name, arguments, swgId) => { command = name; target = swgId; }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
                interactions.SelectMenuAction(2, ObjectMenuAction.VehicleEnterExit.Id, 1);
                Assert.That(command, Is.EqualTo("mount"));
                Assert.That(target, Is.EqualTo((SwgId)2));
                Assert.That(player.Mount, Is.Null);
                registry.Reparent(1, 2);
                interactions.SelectMenuAction(2, ObjectMenuAction.VehicleEnterExit.Id, 1);
                Assert.That(command, Is.EqualTo("dismount"));
                Assert.That(player.Mount, Is.SameAs(vehicle));
                Assert.That(callbacks, Is.Zero, "Core3 does not handle action 205 as ObjectMenuSelect.");
                interactions.SelectMenuAction(2, ObjectMenuAction.VehicleEnterExit.Id, 3);
                Assert.That(callbacks, Is.EqualTo(1), "An explicitly server-owned callback is still forwarded unchanged.");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void MountedCoordinatesAndServerCorrectionsPreserveTheMountUntilDismount()
        {
            var registry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(registry);
            var state = new WorldState(registry);
            currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/test.trn");
            var player = new CreatureSceneObject { SwgId = 1, Position = new Vector3(100, 5, 200), States = CreatureState.RidingMount };
            var vehicle = new CreatureSceneObject { SwgId = 2, Position = player.Position };
            registry.Add(player); registry.Add(vehicle); registry.Reparent(1, 2);
            Assert.That(registry.GetServerWorldPose(1).position, Is.EqualTo(player.Position), "Mounted positions are absolute, not added twice.");
            var item = new TangibleSceneObject { SwgId = 3, Position = Vector3.right };
            registry.Add(item); registry.Reparent(3, 1);
            Assert.That(registry.GetServerWorldPose(3).position, Is.EqualTo(player.Position + Vector3.right), "Objects contained by a mounted rider must not add the vehicle position twice either.");
            var replication = GameServicesForTests.CreateReplication(state, currentPlayer);
            try
            {
                var original = vehicle.Position;
                replication.Commit(2, new MovementUpdate(new Vector3(115, 5, 200), Quaternion.identity, 0, 1), isBroadcast: true);
                replication.ApplyPending();
                Assert.That(vehicle.Position, Is.EqualTo(original), "The locally controlled mount must not consume its own extrapolated broadcasts.");
                var correction = new Vector3(110, 5, 205);
                replication.Commit(1, new MovementUpdate(correction, Quaternion.identity, 0, 1));
                replication.ApplyPending();
                Assert.That(player.Mount, Is.SameAs(vehicle));
                Assert.That(vehicle.Position, Is.EqualTo(correction));
                Assert.That(player.Position, Is.EqualTo(correction));
                replication.Commit(1, new CreatureSceneObjectUpdate { States = 0 });
                replication.Commit(2, new MovementUpdate(original, Quaternion.identity, 0, 2), isBroadcast: true);
                replication.ApplyPending();
                Assert.That(vehicle.Position, Is.EqualTo(original), "The stopped vehicle broadcast applies as soon as the riding state is cleared.");
                replication.Commit(1, new MovementUpdate(correction, Quaternion.identity, 0, 2));
                replication.ApplyPending();
                Assert.That(player.Mount, Is.Null);
            }
            finally { Object.DestroyImmediate(replication.gameObject); }
        }
    }
}
