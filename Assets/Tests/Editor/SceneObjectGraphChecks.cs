using System;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Tests.Editor
{
    public sealed class SceneObjectGraphChecks
    {
        [Test]
        public void ReparentUpdatesBothSidesAndCanDetachToRoot()
        {
            var registry = new SceneObjectRegistry();
            var first = new SceneObject
            {
                SwgId = 1
            };
            var second = new SceneObject
            {
                SwgId = 2
            };
            var child = new SceneObject
            {
                SwgId = 3,
                ParentSwgId = 1
            };
            registry.Add(first);
            registry.Add(second);
            registry.Add(child);
            registry.Reparent(3, 2);
            Assert.That(first.Children, Is.Empty);
            Assert.That(second.Children, Is.EquivalentTo(new[] { child }));
            Assert.That(child.Parent, Is.SameAs(second));
            Assert.That(child.ParentSwgId, Is.EqualTo(new SwgId(2)));
            registry.Reparent(3, SwgId.None);
            Assert.That(second.Children, Is.Empty);
            Assert.That(child.Parent, Is.Null);
        }

        [Test]
        public void ChildrenCanArriveBeforeTheirParentAndMoveBeforeItArrives()
        {
            var registry = new SceneObjectRegistry();
            var child = new SceneObject
            {
                SwgId = 3,
                ParentSwgId = 1
            };
            registry.Add(child);
            Assert.That(child.Parent, Is.Null);
            registry.Reparent(3, 2);
            registry.Add(new SceneObject { SwgId = 1 });
            Assert.That(registry.TryGet(1, out var foundObject1), Is.True);
            Assert.That(foundObject1.Children, Is.Empty);
            var parent = new SceneObject
            {
                SwgId = 2
            };
            registry.Add(parent);
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(parent.Children, Is.EquivalentTo(new[] { child }));
        }

        [Test]
        public void RefiningAPlaceholderPreservesItsParentAndChildren()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SceneObject { SwgId = 1 });
            registry.Add(new SpatialSceneObject { SwgId = 2, ParentSwgId = 1 });
            var child = new SceneObject
            {
                SwgId = 3,
                ParentSwgId = 2
            };
            registry.Add(child);
            var refined = new CreatureSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1
            };
            registry.Replace(refined);
            Assert.That(refined, Is.TypeOf<CreatureSceneObject>());
            Assert.That(registry.TryGet(1, out var foundObject2), Is.True);
            Assert.That(foundObject2.Children, Is.EquivalentTo(new[] { refined }));
            Assert.That(refined.Children, Is.EquivalentTo(new[] { child }));
            Assert.That(child.Parent, Is.SameAs(refined));
        }

        [Test]
        public void ContainmentCyclesAreRejectedWithoutChangingExistingLinks()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SceneObject { SwgId = 1 });
            registry.Add(new SceneObject { SwgId = 2, ParentSwgId = 1 });
            Assert.Throws<InvalidOperationException>(() => registry.Reparent(1, 2));
            Assert.That(registry.TryGet(1, out var foundObject3), Is.True);
            Assert.That(foundObject3.ParentSwgId.IsNone, Is.True);
            Assert.That(registry.TryGet(2, out var foundObject4), Is.True);
            Assert.That(foundObject4.Parent, Is.SameAs(foundObject3));
        }

        [Test]
        public void ClearRemovesPendingLinksBeforeIdsAreReused()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SceneObject { SwgId = 2, ParentSwgId = 1 });
            registry.Clear();
            registry.Add(new SceneObject { SwgId = 1 });
            Assert.That(registry.TryGet(1, out var foundObject5), Is.True);
            Assert.That(foundObject5.Children, Is.Empty);
        }
    }
}
