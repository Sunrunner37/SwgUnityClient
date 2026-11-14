using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Materials;
using Swg.Client.Core.Configuration;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class CharacterAppearanceChecks
    {
        static TreAssetLoader Assets()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            return new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
        }

        [Test]
        public void DressedCaptainLoadsClientAuthoredOutfitAndFaceSettings()
        {
            using var loader = Assets();
            const string template = "object/mobile/shared_dressed_imperial_captain_m.iff";
            var clientData = CharacterClientData.Load(loader, template);
            Assert.That(clientData.Wearables.SelectMany(item => item.Meshes).Count(), Is.EqualTo(5));
            Assert.That(clientData.Customization["/private/index_style_beard"], Is.EqualTo(9));
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, template);
            var meshes = model.Sources.Select(source => source.MeshPath).Distinct().ToArray();
            foreach (string mesh in new[] { "hat_imperial_s01_m", "rank_imp_captain_m", "jacket_s03_m", "pants_s15_m", "boots_s12_m" })
                Assert.That(meshes, Does.Contain("appearance/mesh/" + mesh + "_l0.mgn"));
            Assert.That(meshes, Does.Not.Contain("appearance/mesh/hum_m_body_l0.mgn"), "Uniform should occlude the bare torso");
        }

        [Test]
        public void TrooperWithExplicitlyEmptyClientDataStillLoadsItsAppearance()
        {
            using var loader = Assets();
            const string template = "object/mobile/shared_dark_trooper.iff";
            var clientData = CharacterClientData.Load(loader, template);
            Assert.That(clientData.Wearables, Is.Empty);
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, template);
            Assert.That(model.Parts, Is.Not.Empty);
            Assert.That(model.Parts.All(part => part.Triangles.Length > 0 && part.Skinning != null), Is.True);
        }

        [Test]
        public void DressedNobleAcceptsNativePaletteCustomization()
        {
            using var loader = Assets();
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, "object/mobile/shared_dressed_noble_fat_human_male_01.iff");
            Assert.That(model.Sources.Select(source => source.MeshPath), Does.Contain("appearance/mesh/robe_s01_m_l0.mgn"));
            Assert.That(model.Parts.All(part => part.Diffuse != null), Is.True);
        }

        [TestCase("appearance/pt_ambient_butterfly.sat", "appearance/mesh/pt_ambient_butterfly_01.mgn", "appearance/animation/butterfly_loc_fly.ans")]
        [TestCase("object/mobile/shared_player_transport.iff", "appearance/mesh/player_transport.mgn", "appearance/animation/transport_idl_ground.ans")]
        public void DirectSkeletalMeshesLoadWithTheirNativeNeutralAnimation(string path, string mesh, string idle)
        {
            using var loader = Assets();
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, path);
            Assert.That(model.Sources.Select(source => source.MeshPath), Does.Contain(mesh));
            Assert.That(model.Parts.Single().Skinning.Skeleton.IdleAnimationPath, Is.EqualTo(idle));
            var appearance = AppearanceLoader.LoadDetailed(loader, path);
            Assert.That(appearance.Warnings, Is.Empty);
            Assert.That(appearance.Parts.Count, Is.EqualTo(model.Parts.Count));
        }

        [TestCase("hum_m", 1, "index_style_beard", 9)]
        [TestCase("zab_f", 0, "index_style_tattoo", 10)]
        public void NativeFaceRecipeCompositesCustomizationWithoutChangingSourceTextures(string species, int slot, string variable, short style)
        {
            using var loader = Assets();
            var mesh = IffNode.Form(IffNode.Parse(loader.Read("appearance/mesh/" + species + "_head_l0.mgn")), "SKMG").Children.Single(node => node.Kind == "0004");
            var own = new Dictionary<string, short>();
            var owner = new Dictionary<string, short> { ["/shared_owner/index_color_skin"] = 0 };
            var textures = new Dictionary<string, TextureData>();
            Assert.That(CharacterTextureRenderer.TryBake(loader, mesh, slot, own, owner, textures, out var original), Is.True);
            var sourcePixels = textures.ToDictionary(pair => pair.Key, pair => pair.Value.Pixels.ToArray());
            own["/private/" + variable] = style;
            Assert.That(CharacterTextureRenderer.TryBake(loader, mesh, slot, own, owner, textures, out var customized), Is.True);
            Assert.That(customized.Width, Is.EqualTo(128));
            Assert.That(customized.Height, Is.EqualTo(256));
            Assert.That(original.Pixels.SequenceEqual(customized.Pixels), Is.False, "Customization should select a different facial layer");
            Assert.That(customized.Pixels.Where((value, index) => index % 4 == 3).All(alpha => alpha > 0), Is.True, "Recipe should cover the full face target without raster gaps");
            foreach (var pair in sourcePixels)
                Assert.That(textures[pair.Key].Pixels, Is.EqualTo(pair.Value), "Baking mutated source DDS " + pair.Key);
        }
    }
}

