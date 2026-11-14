using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class VehicleMotionChecks
    {
        [TestCase("object/mobile/vehicle/shared_landspeeder_x31.iff", 7, 11)]
        [TestCase("object/mobile/vehicle/shared_speederbike_swoop.iff", 10, 17)]
        [Category("NativeAssets")]
        public void NativeAccelerationRampsToSpeedAndBrakesWithoutOvershoot(string path, float acceleration, float maximum)
        {
            using var loader = OpenAssets();
            var movement = new ObjectTemplateCatalog(loader).CreatureMovement(path);
            Assert.That(movement.RunAcceleration, Is.EqualTo(acceleration));
            Assert.That(movement.RunSpeed, Is.EqualTo(maximum));
            float speed = 0;
            for (int frame = 0; frame < 60; frame++)
                speed = PlayerMovement.AdvanceVehicleSpeed(speed, maximum, movement.RunAcceleration, 1f / 60);
            Assert.That(speed, Is.EqualTo(acceleration).Within(.001f));
            Assert.That(speed, Is.LessThan(maximum));
            speed = PlayerMovement.AdvanceVehicleSpeed(speed, maximum, acceleration, 10);
            Assert.That(speed, Is.EqualTo(maximum));
            Assert.That(PlayerMovement.AdvanceVehicleSpeed(speed, 0, acceleration, .25f), Is.EqualTo(maximum - acceleration * .25f));
            Assert.That(PlayerMovement.AdvanceVehicleSpeed(speed, 0, acceleration, 10), Is.Zero);
        }

        [Test]
        public void AccelerationModifierDeltasReachTheCreature()
        {
            var update = new CreatureSceneObjectUpdate();
            Assert.That(CreatureView4.ReadDeltaField(0, new ByteReader(BitConverter.GetBytes(.75f)), update), Is.True);
            Assert.That(CreatureView4.ReadDeltaField(1, new ByteReader(BitConverter.GetBytes(1.5f)), update), Is.True);
            var creature = new CreatureSceneObject();
            creature.Apply(update);
            Assert.That(creature.AccelerationMultiplierBase * creature.AccelerationMultiplier, Is.EqualTo(1.125f));
        }

        [TestCase("object/mobile/vehicle/shared_landspeeder_x31.iff", 3)]
        [TestCase("object/mobile/vehicle/shared_speederbike_swoop.iff", 4)]
        [Category("NativeAssets")]
        public void ExhaustScalesFromIdleWithoutMovingHardpointsOrChangingSharedAssets(string path, int count)
        {
            using var loader = OpenAssets();
            var appearance = AppearanceLoader.LoadDetailed(loader, path);
            Assert.That(appearance.Particles.Count(effect => effect.IsVehicleExhaust), Is.EqualTo(count));
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini")));
            var materials = new SwgMaterialFactory(assets);
            using var resources = new AppearanceResources(appearance, materials, Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            var second = resources.Create(null);
            try
            {
                var exhaust = root.GetComponent<VehicleExhaust>();
                var effects = root.transform.Cast<Transform>().Where(child => child.name.EndsWith(".prt")).ToArray();
                Assert.That(effects.Length, Is.EqualTo(count));
                var positions = effects.Select(effect => effect.localPosition).ToArray();
                var idle = effects.Select(effect => effect.localScale).ToArray();
                Assert.That(exhaust.Throttle, Is.Zero);
                exhaust.ApplyThrottle(1);
                for (int i = 0; i < effects.Length; i++)
                {
                    Assert.That(effects[i].localScale.x / idle[i].x, Is.EqualTo(1f / .15f).Within(.001f));
                    Assert.That(effects[i].localPosition, Is.EqualTo(positions[i]));
                }
                Assert.That(second.GetComponent<VehicleExhaust>().Throttle, Is.Zero, "Instances must not share throttle state.");
                exhaust.ApplyThrottle(0);
                Assert.That(effects.Select(effect => effect.localScale), Is.EqualTo(idle));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        static TreAssetLoader OpenAssets()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            return new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
        }
    }
}
