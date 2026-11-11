using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using System;
using System.Threading.Tasks;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Runtime;
using Swg.Client.Runtime.World;
using UnityEngine;

public static class VerifyAutomaticStreaming
{
    public static async Task<object> Run()
    {
        var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
        var session = GameServices.Resolve<GameReplication>();
        var packets = new GamePacketRouter(GameServices.Resolve<Autofac.ILifetimeScope>());
        var original = world.Streaming.ServerCenter;
        var moved = original + new Vector3(256, 0, 0);
        using var writer = new PacketWriter();
        var packet = new UpdateTransformPacket((ulong)world.State.PlayerSwgId, moved, 200, 0, 0);
        packets.Accept(writer.Encode(in packet));
        await Wait(world, moved);
        int movedRegions = world.Streaming.CompletedRegions;
        packet = new UpdateTransformPacket((ulong)world.State.PlayerSwgId, original, 201, 0, 0);
        packets.Accept(writer.Encode(in packet));
        await Wait(world, original);
        return new
        {
            automaticMovementStreaming = true,
            movedRegions,
            returnedRegions = world.Streaming.CompletedRegions
        };
    }

    static async Task Wait(WorldController world, Vector3 center)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while ((world.Streaming.ServerCenter - center).sqrMagnitude > 1)
        {
            if (world.Streaming.Failure != null)
                throw new Exception(world.Streaming.Failure);
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Player movement did not trigger streaming.");
            await Task.Delay(25);
        }
    }
}
