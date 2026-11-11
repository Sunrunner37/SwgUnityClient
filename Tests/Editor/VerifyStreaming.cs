using Swg.Client.Core;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Swg.Client.Core.Assets.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime.World;
using UnityEngine;

public static class VerifyStreaming
{
    public static async Task<object> Run()
    {
        var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
        if (world == null || !world.IsReady)
            throw new Exception("Start in a ready world.");
        world.enabled = false;
        try
        {
            var streaming = world.Streaming;
            var center = streaming.ServerCenter;
            var before = ResidentModels(world);
            var originalTiles = world.GetComponentsInChildren<MeshFilter>().Where(filter => filter.name == "Native terrain").Select(filter => filter.GetEntityId()).ToHashSet();
            int initialRegions = streaming.CompletedRegions;
            streaming.Update(center);
            if (streaming.IsBusy || streaming.CompletedRegions != initialRegions)
                throw new Exception("Stationary player reloaded the region.");
            var movedCenter = center + new Vector3(256, 0, 0);
            streaming.Update(movedCenter);
            await Wait(streaming, initialRegions + 1);
            await Task.Delay(50);
            var moved = ResidentModels(world);
            var movedTiles = world.GetComponentsInChildren<MeshFilter>().Where(filter => filter.name == "Native terrain").Select(filter => filter.GetEntityId()).ToHashSet();
            int retainedTiles = movedTiles.Intersect(originalTiles).Count();
            int newScenery = moved.Keys.Except(before.Keys).Count();
            int removedScenery = before.Keys.Except(moved.Keys).Count();
            int reusedModels = moved.Keys.Intersect(before.Keys).Count(id => ReferenceEquals(before[id], moved[id]));
            if (retainedTiles == 0 || retainedTiles >= movedTiles.Count || newScenery == 0 || removedScenery == 0 || reusedModels == 0)
                throw new Exception("Movement did not incrementally retain/add/remove terrain and scenery.");
            if (movedTiles.Count != streaming.TerrainTileCount)
                throw new Exception("Unloaded terrain objects survived.");
            var desired = world.State.Snapshot.Query(movedCenter, 512, 256).Select(entry => entry.SwgId).ToHashSet();
            if (moved.Keys.Any(id => !desired.Contains(id)))
                throw new Exception("Distant scenery stayed resident.");
            streaming.Update(center);
            await Wait(streaming, initialRegions + 2);
            await Task.Delay(50);
            if (!before.Keys.ToHashSet().SetEquals(ResidentModels(world).Keys))
                throw new Exception("Returning did not restore the original scenery set.");
            if (world.GetComponentsInChildren<MeshFilter>().Count(filter => filter.name == "Native terrain") != originalTiles.Count)
                throw new Exception("Terrain residency grew after returning.");
            if (ResidentModels(world).Any(pair => !ReferenceEquals(pair.Value, before[pair.Key])))
                throw new Exception("Returning decoded cached models again.");
            return new
            {
                initialTiles = originalTiles.Count,
                retainedTiles,
                newScenery,
                removedScenery,
                reusedModels,
                regions = streaming.CompletedRegions,
                returnedToOriginalResidency = true
            };
        }
        finally
        {
            if (world != null)
                world.enabled = true;
        }
    }

    static Dictionary<SwgId, ModelData> ResidentModels(WorldController world)
    {
        var renderer = world.GetComponentInChildren<WorldSceneObjectRenderer>();
        var scenery = (IDictionary)typeof(WorldSceneObjectRenderer).GetField("models", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
        var result = new Dictionary<SwgId, ModelData>();
        foreach (DictionaryEntry pair in scenery)
            if (world.State.Snapshot.ById.ContainsKey((SwgId)pair.Key))
                result.Add((SwgId)pair.Key, (ModelData)pair.Value);
        return result;
    }

    static async Task Wait(WorldStreamer streaming, int regions)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (streaming.CompletedRegions < regions)
        {
            if (streaming.Failure != null)
                throw new Exception(streaming.Failure);
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Streaming did not complete.");
            // Polling also surfaces asynchronous streaming errors without requesting a different region.
            streaming.Update(streaming.ServerCenter);
            await Task.Delay(25);
        }
    }
}
