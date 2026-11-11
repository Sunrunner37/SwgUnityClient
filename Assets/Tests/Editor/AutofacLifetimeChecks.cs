using Swg.Client.Runtime.Networking;
using Autofac;
using NUnit.Framework;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Tests.Editor
{
    public sealed class AutofacLifetimeChecks
    {
        [Test]
        public void GameScopesShareGlobalInstancesButIsolateWorldState()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new object ());
            using var root = builder.Build();
            using var first = root.BeginLifetimeScope(RegisterGame);
            using var second = root.BeginLifetimeScope(RegisterGame);
            Assert.That(first.Resolve<object>(), Is.SameAs(second.Resolve<object>()));
            Assert.That(first.Resolve<WorldState>(), Is.SameAs(first.Resolve<WorldState>()));
            Assert.That(first.Resolve<WorldState>(), Is.Not.SameAs(second.Resolve<WorldState>()));
            Assert.That(first.Resolve<GameReplication>().State, Is.SameAs(first.Resolve<WorldState>()));
            Assert.That(root.IsRegistered<WorldState>(), Is.False);
        }

        [Test]
        public void GameScopeDisposalStopsReplicationAndClearsRegistryListeners()
        {
            using var root = new ContainerBuilder().Build();
            var scope = root.BeginLifetimeScope(RegisterGame);
            var registry = scope.Resolve<SceneObjectRegistry>();
            var replication = scope.Resolve<GameReplication>();
            registry.Add(new SceneObject { SwgId = 1 });
            int calls = 0;
            registry.Subscribe(1, changes => calls++);
            scope.Dispose();
            Assert.That(registry.Count, Is.Zero);
            registry.Add(new SceneObject { SwgId = 1 });
            Assert.That(calls, Is.Zero);
            replication.ApplyPending();
        }

        [Test]
        public void GameLifetimeOwnsSceneResourcesAndWorldState()
        {
            using var root = new ContainerBuilder().Build();
            var game = root.BeginLifetimeScope(builder =>
            {
                RegisterGame(builder);
                builder.RegisterType<SceneResource>().SingleInstance();
            });
            var state = game.Resolve<WorldState>();
            var resource = game.Resolve<SceneResource>();
            state.SceneObjects.Add(new SceneObject { SwgId = 1 });
            Assert.That(game.Resolve<SceneResource>(), Is.SameAs(resource));
            game.Dispose();
            Assert.That(resource.Disposed, Is.True);
            Assert.That(state.SceneObjects.Count, Is.Zero);
        }

        public sealed class SceneResource : System.IDisposable
        {
            public bool Disposed { get; private set; }

            public SceneResource()
            {
            }

            public void Dispose() => Disposed = true;
        }

        static void RegisterGame(ContainerBuilder builder)
        {
            builder.RegisterType<SceneObjectRegistry>().SingleInstance().OnRelease(value => value.Clear());
            builder.RegisterType<WorldState>().SingleInstance();
            builder.Register(context => GameServicesForTests.CreateReplication(context.Resolve<WorldState>())).SingleInstance();
        }
    }
}
