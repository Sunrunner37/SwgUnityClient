using Swg.Client.Core.Networking.Game.Packets.Replication;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Replication
{
    [Preserve]
    internal sealed class BaselinesPacketHandler : GamePacketHandler<BaselinesPacket>
    {
        readonly GameWorldState world;
        readonly Dictionary<(ReplicatedObjectType Type, byte View), Func<ByteReader, GameObjectUpdate>> baselines;
        public override uint Opcode => BaselinesPacket.Opcode;

        [Preserve]
        public BaselinesPacketHandler(GameWorldState world) : base(BaselinesPacket.ReadFrom)
        {
            this.world = world;
            baselines = new Dictionary<(ReplicatedObjectType, byte), Func<ByteReader, GameObjectUpdate>>
            {
                [(ReplicatedObjectType.Tangible, 3)] = TangibleView3.ReadBaseline,
                [(ReplicatedObjectType.Creature, 3)] = CreatureView3.ReadBaseline,
                [(ReplicatedObjectType.Creature, 6)] = CreatureView6.ReadBaseline
            };
        }

        protected override void Handle(BaselinesPacket packet)
        {
            if (!baselines.TryGetValue(((ReplicatedObjectType)packet.ObjectType, packet.View), out var read))
            {
                Debug.LogWarning("Unsupported baseline type 0x" + packet.ObjectType.ToString("X8") + " view " + packet.View);
                // Preserve identity even when this object's view is not implemented yet.
                world.Commit(packet.ObjectId, new GameObjectUpdate { BaselineType = packet.ObjectType });
                return;
            }

            var reader = new ByteReader(packet.Payload);
            reader.U16(); // Baseline operand count.
            var update = read(reader);
            ReplicationReader.RequireEnd(reader);
            update.BaselineType = packet.ObjectType;
            world.Commit(packet.ObjectId, update);
        }
    }
}
