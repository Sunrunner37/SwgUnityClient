using Swg.Client.Runtime.World.SceneObjects;
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class SceneObjectSubscriptionChecks
    {
        static int Entries(SceneObjectRegistry registry) => ((IDictionary)typeof(SceneObjectRegistry).GetField("subscriptions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(registry)).Count;
        [Test]
        public void DispatchCallsOnlyTheRequestedObjectListenersAndReleasesEmptyEntries()
        {
            var registry = new SceneObjectRegistry();
            var sceneObject = new SceneObject
            {
                SwgId = 1
            };
            registry.Add(sceneObject);
            registry.Add(new SceneObject { SwgId = 2 });
            int first = 0;
            int second = 0;
            int unrelated = 0;
            Action<SceneObjectChange> a = changes => first++;
            Action<SceneObjectChange> b = changes => second++;
            Action<SceneObjectChange> other = changes => unrelated++;
            registry.Subscribe(1, a);
            registry.Subscribe(1, b);
            registry.Subscribe(2, other);
            registry.NotifyChanged(sceneObject, SceneObjectChange.Transform);
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(1));
            Assert.That(unrelated, Is.Zero);
            registry.Unsubscribe(1, a);
            registry.NotifyChanged(sceneObject, SceneObjectChange.Transform);
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(2));
            registry.Unsubscribe(1, b);
            Assert.That(Entries(registry), Is.EqualTo(1));
            registry.Unsubscribe(2, other);
            registry.Unsubscribe(2, other);
            Assert.That(Entries(registry), Is.Zero);
        }

        [Test]
        public void SubscriptionsSurvivePlaceholderRefinement()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SpatialSceneObject { SwgId = 1 });
            int calls = 0;
            registry.Subscribe(1, changes => calls++);
            var refined = new CreatureSceneObject
            {
                SwgId = 1
            };
            registry.Replace(refined);
            Assert.That(refined, Is.TypeOf<CreatureSceneObject>());
            Assert.That(calls, Is.GreaterThan(0));
            calls = 0;
            registry.NotifyChanged(refined, SceneObjectChange.Transform);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(Entries(registry), Is.EqualTo(1));
        }

        [Test]
        public void RemovalAndClearReleaseSubscriptionsIncludingObjectsNotYetReceived()
        {
            var registry = new SceneObjectRegistry();
            var sceneObject = new SceneObject
            {
                SwgId = 1
            };
            registry.Add(sceneObject);
            int calls = 0;
            registry.Subscribe(1, changes => calls++);
            registry.Subscribe(2, changes => calls++);
            registry.Remove(1);
            Assert.That(Entries(registry), Is.EqualTo(1));
            registry.Add(sceneObject);
            Assert.That(calls, Is.Zero);
            registry.Clear();
            Assert.That(Entries(registry), Is.Zero);
        }

        [Test]
        public void ManagedBindingsDoNotSubscribeToRegistry()
        {
            var registry = new SceneObjectRegistry();
            var first = new SpatialSceneObject
            {
                SwgId = 1,
                Position = Vector3.right * 10
            };
            var second = new SpatialSceneObject
            {
                SwgId = 2,
                Position = Vector3.right * 20
            };
            registry.Add(first);
            registry.Add(second);
            var instance = new GameObject("direct subscription");
            try
            {
                var binding = instance.AddComponent<ManagedSceneObject>();
                binding.Bind(1);
                Assert.That(instance.GetComponent<SceneObjectMovement>(), Is.Null);
                binding.Unbind();
                binding.Bind(2);
                Assert.That(Entries(registry), Is.Zero);
                registry.NotifyChanged(first, SceneObjectChange.Containment);
                Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero));
                registry.NotifyChanged(second, SceneObjectChange.Containment);
                Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero));
                binding.Unbind();
                Assert.That(Entries(registry), Is.Zero);
                UnityEngine.Object.DestroyImmediate(instance);
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
