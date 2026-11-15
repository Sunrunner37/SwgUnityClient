using Swg.Client.Core.Characters;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Core.World.Terrain;
using System;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Runtime.World;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class WorldStreamingChecks
    {
        [Test]
        public void TerrainRendererOwnsReplacementRetentionAndDisposal()
        {
            var parent = new GameObject("Terrain ownership test");
            var settings = new WorldRenderingContext();
            settings.Initialize(Shader.Find("SWG/World"), null, null, null, null, null);
            var renderer = new WorldTerrainRenderer(new SwgMaterialFactory(null), GameServicesForTests.CreateState(out var currentPlayer), settings);
            var patch = WorldTerrainPatch.Build(new WorldTerrain(), Vector3.zero, 2, 1);
            try
            {
                renderer.AddOrReplace(Vector2Int.zero, patch, parent.transform);
                var original = parent.GetComponentInChildren<MeshFilter>();
                var originalMesh = original.sharedMesh;
                var originalMaterial = original.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(renderer.Resolutions[Vector2Int.zero], Is.EqualTo(1));
                renderer.AddOrReplace(Vector2Int.zero, patch, parent.transform);
                Assert.That(original == null, Is.True);
                Assert.That(originalMesh == null, Is.True);
                Assert.That(originalMaterial == null, Is.True);
                Assert.That(parent.transform.childCount, Is.EqualTo(1));
                renderer.Retain(Array.Empty<Vector2Int>());
                Assert.That(renderer.Keys, Is.Empty);
                Assert.That(renderer.Resolutions, Is.Empty);
                Assert.That(parent.transform.childCount, Is.Zero);
                renderer.AddOrReplace(Vector2Int.one, patch, parent.transform);
                renderer.Dispose();
                Assert.That(parent.transform.childCount, Is.Zero);
                Assert.That(renderer.Keys, Is.Empty);
            }
            finally
            {
                renderer.Dispose();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void MovingOneTileRetainsOverlapAndRequestsOnlyNewTerrain()
        {
            var first = WorldRegionPlanner.TerrainKeys(Vector3.zero, 256, 256).ToHashSet();
            var moved = WorldRegionPlanner.TerrainKeys(new Vector3(256, 0, 0), 256, 256).ToHashSet();
            Assert.That(first.Count, Is.EqualTo(9));
            Assert.That(moved.Intersect(first).Count(), Is.EqualTo(6));
            Assert.That(moved.Except(first).Count(), Is.EqualTo(3));
            Assert.That(first.Except(moved).Count(), Is.EqualTo(3));
        }

        [Test]
        public void TerrainCoverageUsesFloorForNegativeCoordinates()
        {
            var keys = WorldRegionPlanner.TerrainKeys(new Vector3(-1, 0, -1), 128, 256).ToHashSet();
            Assert.That(keys.SetEquals(new[] { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, 0) }), Is.True);
        }

        [Test]
        public void StreamingSettingsRejectGapsAndMisalignedTerrainSamples()
        {
            var settings = ScriptableObject.CreateInstance<WorldStreamingSettings>();
            try
            {
                Assert.DoesNotThrow(settings.Validate);
                settings.refreshDistance = 300;
                Assert.Throws<InvalidOperationException>(settings.Validate);
                settings.refreshDistance = 64;
                settings.terrainSpacing = 7;
                Assert.Throws<InvalidOperationException>(settings.Validate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
