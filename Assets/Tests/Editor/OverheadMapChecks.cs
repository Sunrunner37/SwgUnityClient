using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.SceneObjects;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Idtl;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MapWidget = Swg.Client.Runtime.UI.Navigation.OverheadMap.OverheadMap;

namespace Swg.Client.Tests.Editor
{
    public sealed class OverheadMapChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true)) write(writer);
            return stream.ToArray();
        }

        [Test]
        public void IndexedRadarGeometryDecodesAndTransformsWithoutRewritingIndices()
        {
            var version = new IffNode { Tag = "FORM", Kind = "0000" };
            version.Children.Add(new IffNode { Tag = "VERT", Data = Bytes(w => { foreach (var v in new[] { Vector3.zero, Vector3.right, Vector3.forward }) { w.Write(v.x); w.Write(v.y); w.Write(v.z); } }) });
            version.Children.Add(new IffNode { Tag = "INDX", Data = Bytes(w => { w.Write(0); w.Write(1); w.Write(2); }) });
            var root = new IffNode { Tag = "FORM", Kind = "IDTL" }; root.Children.Add(version);
            var geometry = IdtlFile.Parse(root);
            var transformed = geometry.Transformed(Matrix4x4.Translate(new Vector3(10, 2, 20)));
            Assert.That(transformed.Vertices[1], Is.EqualTo(new Vector3(11, 2, 20)));
            Assert.That(transformed.Triangles, Is.SameAs(geometry.Triangles));
            Assert.That(geometry.Vertices[1], Is.EqualTo(Vector3.right));
        }

        [Test]
        public void ProjectionUsesCameraHeadingAndIgnoresElevation()
        {
            var origin = new Vector3(100, 20, 200);
            Assert.That(MapWidget.Project(origin + Vector3.forward * 10, origin, 0, 2), Is.EqualTo(new Vector2(0, 20)));
            var east = MapWidget.Project(origin + new Vector3(10, 400, 0), origin, 90, 2);
            Assert.That(east.x, Is.EqualTo(0).Within(.0001f));
            Assert.That(east.y, Is.EqualTo(20).Within(.0001f));
            var north = MapWidget.Project(origin + Vector3.forward * 10, origin, 90, 2);
            Assert.That(north.x, Is.EqualTo(-20).Within(.0001f));
        }

        [TestCase(0u, "white")]
        [TestCase(1u, "yellow")]
        [TestCase(3u, "red")]
        [TestCase(33u, "red")]
        public void CreatureColorsUseViewerRelativeServerFlags(uint flags, string expected)
        {
            var root = new GameObject("Map colors", typeof(RectTransform));
            try
            {
                var map = root.AddComponent<MapWidget>();
                var color = map.ColorFor(new CreatureSceneObject { PvpStatus = flags });
                Assert.That(color, Is.EqualTo(expected == "red" ? map.hostileColor : expected == "yellow" ? map.neutralColor : map.friendlyColor));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void DoorDashSpansTheHorizontalOpeningRatherThanItsVerticalHeight()
        {
            MapWidget.EntranceDash(new[] { Vector3.zero, new Vector3(0, 4, 0), new Vector3(3, 4, 0), new Vector3(3, 0, 0) }, out var a, out var b);
            Assert.That(Mathf.Abs(a.x - b.x), Is.EqualTo(3));
            Assert.That(a.z, Is.EqualTo(b.z));
        }

        [Test]
        public void OverlayRendersCachedBuildingsDoorsAndColoredFacingArrowsWithoutACaptureCamera()
        {
            var roots = new List<GameObject>();
            GameObject Root(string name) { var root = new GameObject(name); roots.Add(root); return root; }
            var registry = new SceneObjectRegistry();
            var state = GameServicesForTests.CreateState(registry, out var player);
            player.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/test.trn");
            GameObject Body(CreatureSceneObject creature)
            {
                registry.Add(creature);
                var body = Root("Creature");
                body.transform.position = creature.Position;
                body.AddComponent<ManagedSceneObject>().Bind(creature.SwgId);
                body.AddComponent<TargetableSceneObject>().Initialize(creature.SwgId, new AppearanceData(), true);
                return body;
            }
            try
            {
                Body(new CreatureSceneObject { SwgId = 1 });
                Body(new CreatureSceneObject { SwgId = 3, Position = Vector3.left * 5, PvpStatus = 1 });
                Body(new CreatureSceneObject { SwgId = 4, Position = Vector3.right * 5, PvpStatus = 0 });
                Body(new CreatureSceneObject { SwgId = 5, Position = Vector3.forward * 5, PvpStatus = 3 });
                registry.Add(new SpatialSceneObject { SwgId = 2 });
                var building = Root("Building"); building.AddComponent<ManagedSceneObject>().Bind(2);
                var renderer = Root("Renderer").AddComponent<WorldSceneObjectRenderer>(); renderer.enabled = false;
                var appearance = new AppearanceData { Radius = 10, PortalLayout = new Swg.Client.Core.Assets.Formats.Pob.PortalLayout() };
                appearance.RadarShapes.Add(new IdtlFile(new[] { Vector3.zero, Vector3.right * 3, Vector3.forward * 3 }, new[] { 0, 1, 2 }));
                appearance.PortalLayout.Entrances.Add(new[] { Vector3.zero, Vector3.up * 3, Vector3.right * 2 });
                ((IDictionary<SwgId, AppearanceData>)renderer.LoadedAppearances).Add(2, appearance);
                var rendering = new WorldRenderingContext(); rendering.Initialize(null, null, null, null, renderer, null);
                var root = Root("Map"); root.AddComponent<RectTransform>();
                var map = root.AddComponent<MapWidget>(); map.rectTransform.sizeDelta = new Vector2(400, 400);
                void Set(string name, object value) => typeof(MapWidget).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(map, value);
                Set("player", player); Set("state", state); Set("rendering", rendering);
                map.Toggle();
                typeof(MapWidget).GetMethod("RefreshMembership", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
                using var mesh = new VertexHelper();
                void Draw() => typeof(MapWidget).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Invoke(map, new object[] { mesh });
                Draw();
                Assert.That(mesh.currentVertCount, Is.EqualTo(27)); // Shape 3, doorway 4, creatures 12, player 8.
                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, 0); Assert.That(vertex.color, Is.EqualTo((Color32)map.buildingColor));
                mesh.PopulateUIVertex(ref vertex, 3); Assert.That(vertex.color, Is.EqualTo((Color32)map.doorColor));
                mesh.PopulateUIVertex(ref vertex, 7); Assert.That(vertex.color, Is.EqualTo((Color32)map.neutralColor));
                mesh.PopulateUIVertex(ref vertex, 11); Assert.That(vertex.color, Is.EqualTo((Color32)map.friendlyColor));
                mesh.PopulateUIVertex(ref vertex, 15); Assert.That(vertex.color, Is.EqualTo((Color32)map.hostileColor));
                mesh.PopulateUIVertex(ref vertex, 19); Assert.That(vertex.color, Is.EqualTo((Color32)Color.white));
                mesh.PopulateUIVertex(ref vertex, 23); Assert.That(vertex.color, Is.EqualTo((Color32)map.playerInnerColor));
                map.Toggle(); Draw(); Assert.That(mesh.currentVertCount, Is.Zero);
            }
            finally { foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void OverlayIsAnEditableNonBlockingHudPrefab()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab");
            var map = hud.GetComponentInChildren<MapWidget>(true);
            Assert.That(map, Is.Not.Null);
            Assert.That(map.raycastTarget, Is.False);
            Assert.That(map.canvasRenderer, Is.Not.Null);
            Assert.That(map.IsOpen, Is.False);
            Assert.That(map.GetComponentInParent<RectMask2D>(true), Is.Not.Null);
            Assert.That(map.GetComponentsInChildren<Camera>(true), Is.Empty);
        }

        [TestCase("object/building/player/shared_player_guildhall_corellia_style_01.iff")]
        [TestCase("object/building/corellia/shared_starport_corellia.iff")]
        [TestCase("object/building/tatooine/shared_cantina_tatooine.iff")]
        [Category("NativeAssets")]
        public void BuildingsLoadAuthoredRadarGeometryAlongsideTheirAppearance(string path)
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var appearance = AppearanceLoader.LoadDetailed(loader, path);
            Assert.That(appearance.Parts, Is.Not.Empty);
            Assert.That(appearance.RadarShapes, Is.Not.Empty, path);
            Assert.That(appearance.RadarShapes.Sum(shape => shape.Triangles.Length), Is.GreaterThan(0));
            Assert.That(appearance.PortalLayout.Entrances, Is.Not.Empty);
            TestContext.WriteLine(path + ": radar vertices=" + appearance.RadarShapes.Sum(shape => shape.Vertices.Length));
        }
    }
}
