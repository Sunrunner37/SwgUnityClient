using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;

namespace Swg.Client.Tests.Editor
{
    [Category("NativeAssets")]
    public sealed class VehiclePresentationChecks
    {
        TreAssetLoader loader;
        [OneTimeSetUp]
        public void OpenAssets()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
        }
        [OneTimeTearDown] public void CloseAssets() => loader.Dispose();

        [Test]
        public void LandspeederPaintUsesTheAlphaMaskAndPreservesUnpaintedMaterials()
        {
            var body = AppearanceLoader.LoadDetailed(loader, "appearance/landspeeder_luke.apt");
            int painted = body.Sources.FindIndex(source => source.ShaderPath == "shader/landspeeder_hcsb21.sht");
            var pixels = body.Parts[painted].Diffuse.Pixels;
            int red = Enumerable.Range(0, pixels.Length / 4).Count(i => pixels[i * 4] > pixels[i * 4 + 1] * 2 && pixels[i * 4] > 40);
            Assert.That(red, Is.GreaterThan(1000), "Native HUEB palette defaults should paint the stripe red.");
            Assert.That(Enumerable.Range(0, pixels.Length / 4).All(i => pixels[i * 4 + 3] == 255), Is.True, "Paint masks must not become opacity.");
            int unpainted = body.Sources.FindIndex(source => source.ShaderPath == "shader/landspeeder_asb14.sht");
            Assert.That(body.Parts[unpainted].Diffuse.Pixels, Is.EqualTo(TextureData.Load(loader, "texture/landspeeder.dds").Pixels));
            var custom = AppearanceLoader.LoadDetailed(loader, "appearance/landspeeder_luke.apt", customization: new Dictionary<string, short> { ["/private/index_color_2"] = 0 });
            Assert.That(custom.Parts[painted].Diffuse.Pixels, Is.Not.EqualTo(pixels), "Replicated customization should override the default trim.");
            var again = AppearanceLoader.LoadDetailed(loader, "appearance/landspeeder_luke.apt");
            Assert.That(again.Parts[painted].Diffuse, Is.SameAs(body.Parts[painted].Diffuse), "Identical paint should reuse the baked texture.");
        }

        [Test]
        public void VehicleExhaustSurvivesSkeletalAndCompoundAppearanceLoading()
        {
            var skeletal = SkeletalAppearanceLoader.LoadDetailed(loader, "object/mobile/vehicle/shared_landspeeder_x31.iff");
            Assert.That(skeletal.Particles.Count, Is.EqualTo(3));
            var appearance = AppearanceLoader.LoadDetailed(loader, "object/mobile/vehicle/shared_landspeeder_x31.iff");
            Assert.That(appearance.Particles.Count, Is.EqualTo(3));
            var positions = appearance.Particles.Select(effect => (Vector3)effect.Transform.GetColumn(3)).ToArray();
            Assert.That(positions.Distinct().Count(), Is.EqualTo(3));
            foreach (var effect in appearance.Particles)
            {
                Assert.That(effect.Path, Is.EqualTo("appearance/pt_landspeeder_thruster.prt"));
                Assert.That(effect.Emitters, Is.Not.Empty);
                Assert.That(effect.Transform.MultiplyVector(Vector3.up).z, Is.LessThan(-.9f), "Native engine hardpoints direct the exhaust behind the vehicle.");
            }
        }
    }
}
