using Swg.Client.Runtime.World.SceneObjects;
using System;
using System.Reflection;
using Autofac;
using Swg.Client.Core.World;
using Swg.Client.Runtime;
using Swg.Client.Runtime.World;

namespace Swg.Client.Tests.Editor
{
    internal sealed class ManagedObjectTestScope : IDisposable
    {
        static readonly FieldInfo game = typeof(GameServices).GetField("game", BindingFlags.Static | BindingFlags.NonPublic);
        readonly object previous;
        readonly ILifetimeScope scope;
        ManagedObjectTestScope(SceneObjectRegistry registry)
        {
            previous = game.GetValue(null);
            var builder = new ContainerBuilder();
            builder.RegisterInstance(registry);
            builder.RegisterInstance(new WorldState(registry));
            scope = builder.Build();
            ManagedSceneObject.ResetPlayerCache();
            game.SetValue(null, scope);
        }

        internal static ManagedObjectTestScope Use(SceneObjectRegistry registry) => new ManagedObjectTestScope(registry);
        public void Dispose()
        {
            game.SetValue(null, previous);
            ManagedSceneObject.ResetPlayerCache();
            scope.Dispose();
        }
    }
}
