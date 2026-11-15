using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Materials;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Tests.Editor
{
    public sealed class WorldInteractionChecks
    {
        [Test] public void CellBaselineRetainsRoomNumberAndUsesTheCore3Type()
        {
            using var bytes = new MemoryStream(); using var writer = new BinaryWriter(bytes);
            writer.Write(0f); writer.Write((ushort)0); writer.Write(0u); writer.Write((ushort)0); writer.Write(0u); writer.Write(0u); writer.Write(7);
            var update = CellSceneObjectUpdate.ReadBaseline(new ByteReader(bytes.ToArray()));
            var cell = (CellSceneObject)ReplicatedSceneObjectFactory.Create(42, null, update.RequiredType);
            update.ApplyTo(cell);
            Assert.That(cell.Number, Is.EqualTo(7));
            Assert.That((uint)ReplicatedObjectType.Cell, Is.EqualTo(0x53434c54));
            Assert.That(SceneObjectFactory.Create(43, "object/cell/shared_cell.iff"), Is.TypeOf<CellSceneObject>());
        }
        [Test] public void ConditionUpdatesRetainDamageAndDoNotChangeHam()
        {
            var tangible = new TangibleSceneObject();
            var update = new TangibleSceneObjectUpdate();
            Assert.That(TangibleView3.ReadDeltaField(8, new ByteReader(System.BitConverter.GetBytes(250)), update), Is.True);
            Assert.That(TangibleView3.ReadDeltaField(9, new ByteReader(System.BitConverter.GetBytes(1000)), update), Is.True);
            tangible.Apply(update);
            Assert.That(tangible.ConditionFraction, Is.EqualTo(.75f));
            Assert.That(update.Changes.HasFlag(SceneObjectChange.Vitals), Is.True);
            Assert.That(((TangibleSceneObject)tangible.Copy()).MaximumCondition, Is.EqualTo(1000));
        }
        [Test] public void NativePaintMaskPreservesTheSecondColorRatherThanTintingEverything()
        {
            var source = new TextureData(2, 1);
            source.Pixels[0] = source.Pixels[1] = source.Pixels[2] = 255;
            source.Pixels[4] = source.Pixels[5] = source.Pixels[6] = source.Pixels[7] = 255;
            var painted = CharacterMaterialLoader.ApplyTwoColor(source, new byte[] { 80, 80, 80 }, new byte[] { 255, 255, 255 });
            Assert.That(painted.Pixels.Take(4), Is.EqualTo(new byte[] { 80, 80, 80, 255 }));
            Assert.That(painted.Pixels.Skip(4), Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
        }
        [Test, Category("NativeAssets")] public void GuildHallContainsWalkableRoomFloorsAndEntrancePolygons()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var appearance = AppearanceLoader.LoadDetailed(loader, "appearance/mun_all_guild_commerce_s01.pob");
            Assert.That(appearance.PortalLayout.Cells.Count(cell => cell.Floor != null), Is.EqualTo(10));
            Assert.That(appearance.PortalLayout.Entrances.Count, Is.GreaterThan(0));
            foreach (var floor in appearance.PortalLayout.Cells.Where(cell => cell.Floor != null).Select(cell => cell.Floor))
                Assert.That(floor.Triangles.All(index => index >= 0 && index < floor.Vertices.Length), Is.True);
        }
        [Test, Category("NativeAssets")] public void FemalePlayerIncludesNativeModestyMeshesWithoutEquipmentObjects()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var appearance = SkeletalAppearanceLoader.LoadDetailed(loader, "object/creature/player/shared_human_female.iff");
            Assert.That(appearance.Sources.Any(source => source.MeshPath.Contains("underwear_bottom_f")), Is.True);
            Assert.That(appearance.Sources.Any(source => source.MeshPath.Contains("underwear_top_f")), Is.True);
        }
    }
}
