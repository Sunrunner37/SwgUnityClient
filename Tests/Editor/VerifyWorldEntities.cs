using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens.CharacterList;
using Swg.Client.Runtime.UI.Screens.Loading;
using Swg.Client.Runtime.World;
using UnityEngine;

public static class VerifyWorldEntities
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<object> Run()
    {
        if (UnityEngine.Object.FindAnyObjectByType<WorldController>()?.IsReady != true)
        {
            var list = UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>();
            if (list == null || !list.IsReady)
                throw new Exception("Start from the ready character list");
            list.next.onClick.Invoke();
            await Until(() => UnityEngine.Object.FindAnyObjectByType<WorldController>()?.IsReady == true && UnityEngine.Object.FindAnyObjectByType<LoadingScreen>() == null);
        }

        var controller = UnityEngine.Object.FindAnyObjectByType<WorldController>();
        var renderer = controller.GetComponentInChildren<WorldSceneObjectRenderer>();
        var source = GameServices.Resolve<GameReplication>();
        var busy = typeof(WorldSceneObjectRenderer).GetField("loading", Private);
        var dirty = typeof(WorldSceneObjectRenderer).GetField("dirtyAppearances", Private);
        await Until(() => !(bool)busy.GetValue(renderer) && ((System.Collections.Generic.HashSet<SwgId>)dirty.GetValue(renderer)).Count == 0);
        var originalState = source.CaptureSnapshot();
        var player = (CreatureSceneObject)originalState.Objects.Single(state => state.SwgId == originalState.PlayerSwgId);
        var original = controller.SceneObjects.GetServerWorldPose(player.SwgId).position;
        var destination = original + new Vector3(256, 0, 0);
        var moved = new Vector3((short)(destination.x * 4) / 4f, (short)(destination.y * 4) / 4f, (short)(destination.z * 4) / 4f);
        var originalTarget = ManagedSceneObject.CurrentPlayer.transform;
        int originalCount = renderer.Count;
        var readGate = (SemaphoreSlim)typeof(WorldContentLoader).GetField("reads", Private).GetValue(controller.Content);
        const ulong temporaryId = 0x7f00000000001234;
        bool held = false;
        bool movementWhileLoading = false;
        bool staleAppearanceDiscarded = false;
        try
        {
            await readGate.WaitAsync();
            held = true;
            Commit(source, player.SwgId, "Customization", new byte[] { 1, 1, 1, 2, 255, 3 });
            Commit(source, temporaryId, "TemplateCrc", ObjectTemplateNames.TemplateCrc("object/tangible/terminal/shared_terminal_bazaar.iff"));
            await Until(() => (bool)busy.GetValue(renderer));
            Move(source, player.SwgId, moved, 501);
            await Until(() => (controller.SceneObjects.GetServerWorldPose(player.SwgId).position - moved).sqrMagnitude < .01f && (originalTarget.localPosition + controller.State.RenderOrigin - moved).sqrMagnitude < .01f);
            movementWhileLoading = (bool)busy.GetValue(renderer) && ManagedSceneObject.CurrentPlayer.transform == originalTarget && originalTarget.gameObject.activeSelf;
            if (!movementWhileLoading)
                throw new Exception("Movement stopped or the player disappeared during appearance decoding");
            Commit(source, player.SwgId, "Customization", player.Customization);
            typeof(GameReplication).GetMethod("Remove", Private).Invoke(source, new object[] { (SwgId)temporaryId });
            await Until(() => !source.SceneObjects.TryGet(temporaryId, out _) && source.SceneObjects.TryGet<CreatureSceneObject>(player.SwgId, out var currentPlayer) && currentPlayer.Customization.SequenceEqual(player.Customization) && ((System.Collections.Generic.HashSet<SwgId>)dirty.GetValue(renderer)).Count == 0);
            readGate.Release();
            held = false;
            await Until(() => !(bool)busy.GetValue(renderer));
            staleAppearanceDiscarded = ManagedSceneObject.CurrentPlayer.transform == originalTarget && renderer.Count == originalCount;
            if (!staleAppearanceDiscarded)
                throw new Exception("A stale appearance or removed entity was uploaded: targetPreserved=" + (ManagedSceneObject.CurrentPlayer.transform == originalTarget) + ", count=" + renderer.Count + ", expected=" + originalCount);
            await Until(() => (controller.Streaming.ServerCenter - moved).sqrMagnitude < 1 && !controller.Streaming.IsBusy);
            int streamedRegions = controller.Streaming.CompletedRegions;
            originalTarget.localPosition += new Vector3(512, 0, 0);
            await Task.Delay(700);
            if (controller.Streaming.CompletedRegions != streamedRegions || (controller.SceneObjects.GetServerWorldPose(player.SwgId).position - moved).sqrMagnitude > 1)
                throw new Exception("Streaming followed the rendered transform instead of server position");
            Move(source, player.SwgId, original, 502);
            await Until(() => (controller.Streaming.ServerCenter - original).sqrMagnitude < 1 && !controller.Streaming.IsBusy);
            return new
            {
                movementWhileLoading,
                staleAppearanceDiscarded,
                playerPositionDrivesStreaming = true,
                entities = renderer.Count,
                scenery = controller.StaticObjectCount,
                regions = controller.Streaming.CompletedRegions
            };
        }
        finally
        {
            Commit(source, player.SwgId, "Customization", player.Customization);
            typeof(GameReplication).GetMethod("Remove", Private).Invoke(source, new object[] { (SwgId)temporaryId });
            Move(source, player.SwgId, original, 503);
            if (held)
                readGate.Release();
        }
    }

    static void Commit(GameReplication source, SwgId id, string field, object value)
    {
        var type = typeof(GameReplication).Assembly.GetType("Swg.Client.Core.Networking.Game.Replication.GameObjectUpdate");
        var update = Activator.CreateInstance(type, true);
        type.GetField(field).SetValue(update, value);
        typeof(GameReplication).GetMethod("Commit", Private).Invoke(source, new object[] { id, update });
    }

    static void Move(GameReplication source, SwgId id, Vector3 position, uint counter)
    {
        var packet = new UpdateTransformPacket((ulong)id, position, counter, 0, 0);
        using var writer = new PacketWriter();
        new GamePacketRouter(GameServices.Resolve<Autofac.ILifetimeScope>()).Accept(writer.Encode(in packet));
    }

    static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (!condition())
        {
            var loading = UnityEngine.Object.FindAnyObjectByType<LoadingScreen>();
            if (loading != null && loading.entry.Failure != null)
                throw new Exception(loading.entry.Failure.Message);
            var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
            if (world?.Failure != null || world?.Streaming?.Failure != null)
                throw new Exception(world.Failure ?? world.Streaming.Failure);
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("World entity verification timed out");
            await Task.Delay(25);
        }
    }
}
