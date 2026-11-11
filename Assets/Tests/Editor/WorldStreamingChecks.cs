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
