using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Particles;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Tre;
using NUnit.Framework;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Cef;
using Swg.Client.Core.Assets.Formats.Swh;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class CombatEffectsChecks
    {
        TreAssetLoader loader;
        [OneTimeSetUp]
        public void LoadArchives()
        {
            var settings = ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini"));
            loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
        }
        [OneTimeTearDown]
        public void CloseArchives() => loader.Dispose();
        [TestCase("attacker_warcry", "appearance/pt_warcry_player.prt", 2.5f)]
        [TestCase("attacker_intimidate", "appearance/pt_intimidate_creature_fire.prt", 1.5f)]
        [TestCase("defender_intimidate", "appearance/pt_intimidate_creature_hit.prt", 1f)]
        public void ClientEffectsRetainNativeAppearancesAndEmissionDuration(string effect, string appearance, float seconds)
        {
            var file = CefFile.Parse(loader.Read("clienteffect/combat_special_" + effect + ".cef"));
            Assert.That(file.Appearances.Count, Is.EqualTo(1));
            Assert.That(file.Appearances[0].Path, Is.EqualTo(appearance));
            Assert.That(file.Appearances[0].Duration, Is.EqualTo(seconds));
            var decoded = AppearanceLoader.LoadDetailed(loader, appearance);
            Assert.That(decoded.Particles.Count, Is.GreaterThan(0), "Includes nested emitter versions and child appearances.");
        }
        [Test]
        public void FlameUsesPlaybackScriptHardpointAndCombatDurationOverride()
        {
            var table = CbtmFile.Parse(loader.Read("combat/combat_manager.iff"), loader.Read);
            Assert.That(table.TryGet(ObjectTemplateNames.TemplateCrc("fire_flame_thrower_cone_1_light"), 1, 0, 0, 0, out var playback), Is.True);
            Assert.That(playback.Particles.Length, Is.EqualTo(1));
            Assert.That(playback.Particles[0].Path, Is.EqualTo("appearance/pt_beam_flame_thrower.prt"));
            Assert.That(playback.Particles[0].Hardpoint, Is.EqualTo("muzzle"));
            Assert.That(playback.Particles[0].Duration, Is.EqualTo(1.5f));
            Assert.That(playback.HasTrails, Is.False);
            Assert.That(table.TryGet(ObjectTemplateNames.TemplateCrc("attack_mid_center_light_0"), 1, 0, 0, 0, out var melee), Is.True);
            Assert.That(melee.HasTrails, Is.True);
            Assert.That(melee.Particles, Is.Empty);
        }
        [TestCase("hold_l", 4)]
        [TestCase("hold_r", 8)]
        [TestCase("lankle", 1)]
        [TestCase("rankle", 2)]
        public void NativeSwingNamesRespectTheServerLimbMask(string hardpoint, int mask)
        {
            Assert.That(ClientEffects.TrailMask(hardpoint), Is.EqualTo(mask));
        }
        [Test]
        public void TrailsUseAuthoredSwingAndHitFrames()
        {
            var clip = CompressedAnimationReader.Read(loader.Read("appearance/animation/all_b_cbt_unarmed_standing_ready_punch.ans"));
            var swing = clip.Messages.Single(m => m.Name == "uswing_hold_r");
            var hit = clip.Messages.Single(m => m.Name == "uhit_hold_r");
            Assert.That(swing.Time, Is.EqualTo(8 / clip.FrameRate));
            Assert.That(hit.Time, Is.EqualTo(13 / clip.FrameRate));
            Assert.That(hit.Time, Is.GreaterThan(swing.Time));
        }
        [Test]
        public async Task RigidWeaponResourcesExposeNativeMuzzleAttachment()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            await assets.Ready;
            var appearance = AppearanceLoader.LoadDetailed(loader, "object/weapon/ranged/rifle/shared_rifle_flame_thrower.iff");
            using var resource = new AppearanceResources(appearance, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resource.Create(null);
            try
            {
                Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(0));
                var muzzle = root.transform.Find("muzzle");
                Assert.That(muzzle, Is.Not.Null);
                Assert.That(muzzle.localPosition.z, Is.GreaterThan(.5f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void SwooshRetainsItsShaderColorAlphaAndWidth()
        {
            var swoosh = SwhFile.Parse(loader.Read("appearance/sw_basic_bloody_red.swh"));
            Assert.That(swoosh.Shader, Is.EqualTo("shader/sw_gradient.sht"));
            Assert.That(swoosh.Color.r, Is.GreaterThan(.7f));
            Assert.That(swoosh.Color.g, Is.Zero);
            Assert.That(swoosh.Color.a, Is.EqualTo(.75f));
            Assert.That(swoosh.Width, Is.EqualTo(.06f).Within(.0001f));
        }
    }
}
