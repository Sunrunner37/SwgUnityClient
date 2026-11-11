using System;
using Swg.Client.Core.Geometry;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public static class RigidTransformChecks
    {
        public static void Run()
        {
            float half = (float)Math.Sqrt(.5);
            var parent = new RigidTransform
            {
                Rotation = new Quaternion(0, half, 0, half),
                Translation = new Vector3(2, 3, 4)
            };
            var point = parent.Point(Vector3.right);
            Check(Vector3.Distance(point, new Vector3(2, 3, 3)) < .00001f, "Rotation followed by translation");
            Check(Vector3.Distance(parent.Inverse().Point(point), Vector3.right) < .00001f, "Pose inverse restores the source point");
            var child = new RigidTransform
            {
                Rotation = new Quaternion(half, 0, 0, half),
                Translation = Vector3.forward
            };
            point = (parent * child).Point(Vector3.up);
            Check(Vector3.Distance(point, new Vector3(4, 3, 4)) < .00001f, "Parent-child pose composition");
            Check(Vector3.Distance(RigidTransform.Identity.Point(point), point) < .00001f, "Identity pose preserves positions");
        }

        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }
    }
}
