using NUnit.Framework;
using Swg.Client.Runtime.UI.Targeting;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class TargetIndicatorChecks
    {
        [Test]
        public void SparseStructureIndicatorsFollowItsVerticesRatherThanEmptyBoundingCorners()
        {
            var root = new GameObject("Sparse structure projection camera");
            try
            {
                var camera = root.AddComponent<Camera>();
                camera.pixelRect = new Rect(0, 0, 800, 600);
                camera.aspect = 4f / 3;
                camera.nearClipPlane = .3f;
                camera.fieldOfView = 60;
                var bounds = new Bounds(new Vector3(0, 7, 0), new Vector3(20, 14, 20));
                var geometry = new[]
                {
                    new[]
                    {
                        new Vector3(-5, 0, -5), new Vector3(5, 0, -5),
                        new Vector3(-5, 0, 5), new Vector3(5, 0, 5),
                        new Vector3(0, 14, 0)
                    }
                };
                var matrix = Matrix4x4.Translate(new Vector3(0, -7, 20));
                Assert.That(TargetHud.TryProjectGeometry(camera, matrix, bounds, geometry, out var tight), Is.True);
                Assert.That(TargetHud.TryProjectBounds(camera, matrix, bounds, out var box), Is.True);
                Assert.That(tight.width, Is.LessThan(box.width));
                Assert.That(tight.height, Is.LessThan(box.height));
                var mast = camera.WorldToScreenPoint(matrix.MultiplyPoint3x4(geometry[0][4]));
                Assert.That(tight.yMax, Is.EqualTo(mast.y).Within(.001f));
                Assert.That(tight.yMax, Is.LessThan(camera.pixelRect.yMax));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(0.5f, true)]
        [TestCase(-3f, false)]
        [TestCase(5f, true)]
        public void VisibleStructureBoundsSurviveNearPlaneClipping(float depth, bool visible)
        {
            var root = new GameObject("Indicator test camera");
            try
            {
                var camera = root.AddComponent<Camera>();
                camera.pixelRect = new Rect(0, 0, 800, 600);
                camera.aspect = 4f / 3;
                camera.nearClipPlane = .3f;
                camera.fieldOfView = 60;
                var bounds = new Bounds(new Vector3(0, 0, depth), Vector3.one * 2);
                bool result = TargetHud.TryProjectBounds(camera, Matrix4x4.identity, bounds, out var projected);
                Assert.That(result, Is.EqualTo(visible));
                if (!visible)
                    return;
                Assert.That(projected.width, Is.GreaterThan(0));
                Assert.That(projected.height, Is.GreaterThan(0));
                Assert.That(projected.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(projected.yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(projected.xMax, Is.LessThanOrEqualTo(camera.pixelRect.xMax));
                Assert.That(projected.yMax, Is.LessThanOrEqualTo(camera.pixelRect.yMax));
                if (depth == .5f)
                    Assert.That(projected, Is.EqualTo(camera.pixelRect));
                else
                    Assert.That(projected.width, Is.LessThan(camera.pixelRect.width));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
