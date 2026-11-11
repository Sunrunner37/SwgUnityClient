using Swg.Client.Runtime.World.SceneObjects;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Runtime.World;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class ManagedSceneObjectChecks
    {
        [Test]
        public void BindingResolvesTheRootFromIdAndFromChildColliders()
        {
            var root = new GameObject("Managed scene-object test");
            try
            {
                var binding = root.AddComponent<ManagedSceneObject>();
                binding.Bind(new SwgId(0xfffffffffffffff0));
                var child = new GameObject("Mesh part", typeof(BoxCollider));
                child.transform.SetParent(root.transform);
                Assert.That(ManagedSceneObject.TryGetObjectFrom(binding.SwgId, out var found), Is.True);
                Assert.That(found, Is.SameAs(root));
                Assert.That(child.GetComponentInParent<ManagedSceneObject>(), Is.SameAs(binding));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Assert.That(ManagedSceneObject.TryGetObjectFrom(new SwgId(0xfffffffffffffff0), out var missing), Is.False);
            Assert.That(missing == null, Is.True);
        }

        [Test]
        public void BindingTwiceIsRejectedUntilExplicitlyUnbound()
        {
            var root = new GameObject("Managed retag test");
            try
            {
                var binding = root.AddComponent<ManagedSceneObject>();
                binding.Bind(10001);
                Assert.Throws<System.InvalidOperationException>(() => binding.Bind(10002));
                Assert.That(binding.SwgId, Is.EqualTo(new SwgId(10001)));
                binding.Unbind();
                binding.Bind(10002);
                Assert.That(ManagedSceneObject.TryGetObjectFrom(10001, out _), Is.False);
                Assert.That(ManagedSceneObject.TryGetObjectFrom(10002, out var found), Is.True);
                Assert.That(found, Is.SameAs(root));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DestroyingAnUnboundOldViewDoesNotRemoveItsReplacement()
        {
            var old = new GameObject("Old managed view");
            var replacement = new GameObject("Replacement managed view");
            try
            {
                var previous = old.AddComponent<ManagedSceneObject>();
                previous.Bind(10003);
                previous.Unbind();
                replacement.AddComponent<ManagedSceneObject>().Bind(10003);
                Object.DestroyImmediate(old);
                Assert.That(ManagedSceneObject.TryGetObjectFrom(10003, out var found), Is.True);
                Assert.That(found, Is.SameAs(replacement));
            }
            finally
            {
                if (old != null)
                    Object.DestroyImmediate(old);
                Object.DestroyImmediate(replacement);
            }
        }
    }
}
