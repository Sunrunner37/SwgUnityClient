using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using NUnit.Framework;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core.World;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Combat;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Theming;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.Streaming;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swg.Client.Tests.Editor
{
    public sealed class WorldHandoffChecks
    {
        static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        static object Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [Test]
        public void WorldReplacementKeepsGameServicesAndReleasesOldHudSubscriptions()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry, builder => builder.Register(context =>
                new WorldContentLoader(null, context.Resolve<WorldState>(), null,
                    ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")),
                    new Admission { Username = "handoff", Galaxy = new Galaxy { Name = "Test" } })).SingleInstance());
            var session = GameServices.GameScope;
            var state = session.Resolve<WorldState>();
            var player = session.Resolve<CurrentPlayer>();
            player.BeginScene(42);
            using var commands = new ClientCommands(_ => { }, _ => { }, player);
            using var catalog = new CommandCatalog(Array.Empty<CommandCatalog.Command>(), player.Commands);
            using var queue = new ClientCombatQueue(commands, catalog);
            var theme = ScriptableObject.CreateInstance<UiTheme>();
            var settings = ScriptableObject.CreateInstance<WorldStreamingSettings>();
            var oldScene = EditorSceneManager.NewPreviewScene();
            var newScene = EditorSceneManager.NewPreviewScene();
            var subscriptions = (Dictionary<Swg.Client.Core.SwgId, Action<SceneObjectChange>>)typeof(SceneObjectRegistry)
                .GetField("subscriptions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(registry);
            WorldController Controller(Scene scene)
            {
                var root = new GameObject("Handoff world");
                SceneManager.MoveGameObjectToScene(root, scene);
                var controller = root.AddComponent<WorldController>();
                controller.Initialize(session, new WorldLoadingData(), settings);
                return controller;
            }
            CombatQueueWindow Hud(WorldController controller)
            {
                var root = new GameObject("Handoff HUD");
                root.transform.SetParent(controller.transform);
                var hud = root.AddComponent<CombatQueueWindow>();
                hud.panel = new GameObject("Queue panel");
                hud.panel.transform.SetParent(root.transform);
                hud.theme = theme;
                Set(hud, "state", state);
                Set(hud, "currentPlayer", player);
                Set(hud, "queue", queue);
                Call(hud, "OnEnable");
                return hud;
            }
            try
            {
                var old = Controller(oldScene);
                var oldHud = Hud(old);
                state.BeginScene(Vector3.one, "terrain/talus.trn");
                Assert.That(old.gameObject.activeSelf, Is.True, "HUD teardown must wait for SceneStarted callbacks to finish.");
                Assert.That(subscriptions.ContainsKey(player.SwgId), Is.True);
                Call(oldHud, "OnDisable");
                Assert.That(subscriptions, Is.Empty, "The departing HUD must not retain player listeners.");
                Call(old, "OnDestroy");
                EditorSceneManager.ClosePreviewScene(oldScene);
                oldScene = default;
                Assert.That(GameServices.GameScope, Is.SameAs(session));
                Assert.That(session.Resolve<WorldState>(), Is.SameAs(state));

                var destination = Controller(newScene);
                var newHud = Hud(destination);
                Assert.That(subscriptions[player.SwgId].GetInvocationList().Length, Is.EqualTo(1));
                Assert.That(subscriptions[player.SwgId].Target, Is.SameAs(newHud));
                Call(newHud, "OnDisable");
                // Even ordinary world destruction must not own session shutdown.
                Call(destination, "OnDestroy");
                Assert.That(GameServices.GameScope, Is.SameAs(session));
                Assert.That(session.Resolve<CurrentPlayer>(), Is.SameAs(player));
            }
            finally
            {
                if (oldScene.IsValid()) EditorSceneManager.ClosePreviewScene(oldScene);
                EditorSceneManager.ClosePreviewScene(newScene);
                UnityEngine.Object.DestroyImmediate(theme);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InventoryPreloadWaitsForDestinationMetadataBeforePreparing()
        {
            using var assets = new Swg.Client.Runtime.Assets.Tre.TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var state = GameServicesForTests.CreateState(out var player);
            var loader = new WorldContentLoader(null, state, null,
                ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")),
                new Admission { Username = "handoff", Galaxy = new Galaxy { Name = "Test" } });
            using var inventory = new ClientInventory(state, _ => { }, player);
            using var previews = new ItemPreviewCache(loader, null, null);
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Handoff preload");
            SceneManager.MoveGameObjectToScene(root, scene);
            var warming = root.AddComponent<InventoryPreviewCache>();
            Set(warming, "inventory", inventory);
            Set(warming, "loader", loader);
            Set(warming, "cache", previews);
            Set(warming, "dirty", true);
            int completed = 0;
            warming.Changed += () => completed++;
            try
            {
                state.BeginScene(Vector3.one, "terrain/talus.trn");
                var routine = (IEnumerator)Call(warming, "Warm");
                Assert.That(routine.MoveNext(), Is.True);
                Assert.That(routine.MoveNext(), Is.True);
                var wait = (WaitUntil)routine.Current;
                Assert.That(wait.keepWaiting, Is.True);
                Assert.That(completed, Is.Zero);
                state.Templates = new ObjectTemplateCatalog(assets.Loader);
                Assert.That(wait.keepWaiting, Is.False);
                Assert.That(routine.MoveNext(), Is.True);
                var prepare = (IEnumerator)routine.Current;
                Assert.That(prepare.MoveNext(), Is.False, "An empty inventory needs no asset decoding.");
                Assert.That(routine.MoveNext(), Is.False);
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(warming.IsReady, Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
