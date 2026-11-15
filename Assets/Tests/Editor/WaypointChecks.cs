using Swg.Client.Core.Characters;
using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Runtime.Assets.Particles;
using System.Linq;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.SceneObjects;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Handlers.Replication;
using Swg.Client.Core.Networking.Game.Packets.Replication;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Runtime.UI.Navigation;
using Swg.Client.Runtime.UI.Targeting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Swg.Client.Tests.Editor
{
    public sealed class WaypointChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            write(writer);
            return bytes.ToArray();
        }

        static void Entry(BinaryWriter writer, byte operation, ulong swgId, string name, bool active)
        {
            writer.Write(operation);
            writer.Write(swgId);
            writer.Write(0u); // Legacy cell.
            writer.Write(100f);
            writer.Write(25f); // Height, not north.
            writer.Write(-200f);
            writer.Write(0ul);
            writer.Write(1666541635u); // Corellia.
            writer.Write((uint)name.Length);
            writer.Write(Encoding.Unicode.GetBytes(name));
            writer.Write(swgId);
            writer.Write((byte)WaypointColor.Blue);
            writer.Write((byte)(active ? 1 : 0));
        }

        [Test]
        public void BaselineAndDeltasReachSharedStateOnTheMainThread()
        {
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            var replication = GameServicesForTests.CreateReplication(state, currentPlayer);
            try
            {
                var payload = Bytes(writer =>
                {
                    writer.Write((ushort)7);
                    writer.Write(0u); writer.Write(1u); // XP map.
                    writer.Write(2u); writer.Write(1u);
                    Entry(writer, 0, 42, "First", true);
                    Entry(writer, 0, 43, "Second", false);
                    writer.Write(new byte[40]); // Force, quest vectors, quest map, reserved map.
                });
                var handler = (IGamePacketHandler)new BaselinesPacketHandler(replication);
                handler.Handle(new ByteReader(Bytes(writer => new BaselinesPacket(7, 0x504c4159, 8, payload).WriteTo(writer))));
                Assert.That(currentPlayer.Waypoints, Is.Empty);
                replication.ApplyPending();
                Assert.That(currentPlayer.Waypoints.Count, Is.EqualTo(2));
                Assert.That(currentPlayer.Waypoints[42].Position, Is.EqualTo(new Vector3(100, 25, -200)));
                Assert.That(currentPlayer.Waypoints[43].Active, Is.False);

                var delta = Bytes(writer =>
                {
                    writer.Write((ushort)2);
                    writer.Write((ushort)2); writer.Write(100u); // Force changes precede waypoints.
                    writer.Write((ushort)1);
                    writer.Write(3u); writer.Write(4u);
                    Entry(writer, 2, 42, "Renamed", false);
                    Entry(writer, 1, 43, "Second", false); // Removal includes all fields.
                    Entry(writer, 0, 44, "Third", true);
                });
                var deltaHandler = (IGamePacketHandler)new DeltasPacketHandler(replication);
                deltaHandler.Handle(new ByteReader(Bytes(writer => new DeltasPacket(7, 0x504c4159, 8, delta).WriteTo(writer))));
                Assert.That(currentPlayer.Waypoints[42].Name, Is.EqualTo("First"));
                replication.ApplyPending();
                Assert.That(currentPlayer.Waypoints.Count, Is.EqualTo(2));
                Assert.That(currentPlayer.Waypoints.ContainsKey(43), Is.False);
                Assert.That(currentPlayer.Waypoints[42].Name, Is.EqualTo("Renamed"));
                Assert.That(currentPlayer.Waypoints[42].Active, Is.False);
                Assert.That(currentPlayer.Waypoints[44].Active, Is.True);
            }
            finally { Object.DestroyImmediate(replication.gameObject); }
        }

        [Test]
        public void PlanetChangesPreserveThePlayersWaypointList()
        {
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            currentPlayer.BeginScene(7);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            Assert.That(state.PlanetCrc, Is.EqualTo(1666541635u));
            currentPlayer.UpdateWaypoints(list => list[42] = new Waypoint(42, Vector3.zero, state.PlanetCrc, "Home", WaypointColor.Blue, true));
            currentPlayer.BeginScene(7);
            state.BeginScene(Vector3.zero, "terrain/tatooine.trn");
            Assert.That(currentPlayer.Waypoints.ContainsKey(42), Is.True);
            Assert.That(state.PlanetCrc, Is.Not.EqualTo(currentPlayer.Waypoints[42].PlanetCrc));
        }

        [TestCase(0, 1, 0)]
        [TestCase(1, 0, -90)]
        [TestCase(0, -1, -180)]
        public void BearingsFollowCameraYawWithoutDependingOnPitch(float x, float z, float angle)
        {
            var root = new GameObject("Navigation camera");
            try
            {
                var camera = root.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(40, 90, 0);
                var offset = Quaternion.Euler(0, 90, 0) * new Vector3(x, 100, z);
                var direction = NavigationArrow.Bearing(camera, Vector3.zero, offset);
                Assert.That(Mathf.DeltaAngle(NavigationArrow.Angle(direction), angle), Is.EqualTo(0).Within(.001));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(8, true)] // Only the edge of the selection remains in view.
        [TestCase(20, false)]
        public void OffscreenDecisionIncludesTheEntireSelectionBox(float x, bool visible)
        {
            var root = new GameObject("Target arrow camera");
            try
            {
                var camera = root.AddComponent<Camera>();
                camera.pixelRect = new Rect(0, 0, 800, 600);
                camera.aspect = 4f / 3;
                camera.fieldOfView = 60;
                var bounds = new Bounds(new Vector3(x, 0, 10), new Vector3(4, 4, 4));
                Assert.That(TargetHud.TryProjectBounds(camera, Matrix4x4.identity, bounds, out _), Is.EqualTo(visible));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NativeGlowAppearancesUseAdditiveVelocityAlignedParticles()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            assets.Ready.GetAwaiter().GetResult();
            AppearanceData blue = null;
            foreach (WaypointColor color in Enum.GetValues(typeof(WaypointColor)))
            {
                var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/waypoint/shared_world_waypoint_" + color.ToString().ToLowerInvariant() + ".iff");
                Assert.That(appearance.Particles.Count, Is.GreaterThan(0), color.ToString());
                Assert.That(appearance.Particles.SelectMany(effect => effect.Emitters).All(emitter => emitter.Additive), Is.True);
                if (color == WaypointColor.Blue)
                    blue = appearance;
            }
            using var resources = new AppearanceResources(blue, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            try
            {
                var tube = root.GetComponentsInChildren<ParticleSystem>().Single(system => system.name == "TUBE");
                var renderer = tube.GetComponent<ParticleSystemRenderer>();
                Assert.That(renderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Stretch));
                Assert.That(renderer.sharedMaterial.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.One));
                Assert.That(tube.shape.position.y, Is.EqualTo(250));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void VelocityTextureRotationPreservesIndividualAnimationCells()
        {
            var source = new TextureData(4, 4);
            for (int pixel = 0; pixel < 16; pixel++)
                source.Pixels[pixel * 4] = (byte)pixel;
            var rotated = ParticleEffectResources.RotateVelocityFrames(source, 2);
            int[] expected = { 1, 5, 3, 7, 0, 4, 2, 6, 9, 13, 11, 15, 8, 12, 10, 14 };
            for (int pixel = 0; pixel < 16; pixel++)
                Assert.That(rotated.Pixels[pixel * 4], Is.EqualTo(expected[pixel]));
            Assert.That(source.Pixels[0], Is.Zero);
        }

        [Test]
        public void PrefabsBindBothNavigationViewsToTheWorldCamera()
        {
            var world = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World.prefab");
            var waypoint = world.GetComponentInChildren<WaypointHud>(true);
            var target = world.GetComponentInChildren<TargetHud>(true);
            Assert.That(waypoint.worldCamera, Is.SameAs(target.worldCamera));
            Assert.That(waypoint.rowPrefab.GetComponentsInChildren<UnityEngine.UI.Button>(true), Is.Empty);
            Assert.That(waypoint.arrowPrefab, Is.Not.Null);
            Assert.That(target.directionArrow, Is.Not.Null);
            Assert.That(waypoint.arrowPrefab.radius, Is.EqualTo(110));
            var pillars = world.GetComponentInChildren<WorldController>(true).waypoints;
            Assert.That(pillars.pillarPrefab, Is.Not.Null);
            Assert.That(pillars.pillarPrefab.GetComponent<ParticleSystemRenderer>().renderMode, Is.EqualTo(ParticleSystemRenderMode.VerticalBillboard));
            Assert.That(pillars.pillarPrefab.main.startSpeed.constantMin, Is.GreaterThan(0));
            Assert.That(pillars.pillarPrefab.shape.position.y, Is.Zero);
            Assert.That(pillars.pillarPrefab.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Cone));
            Assert.That(pillars.pillarPrefab.shape.angle, Is.Zero);
        }
    }
}
