using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Replication;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Scripting;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Replication
{
    [Preserve]
    internal sealed class DeltasPacketHandler : GamePacketHandler<DeltasPacket>
    {
        readonly GameReplication replication;
        readonly Dictionary<(ReplicatedObjectType Type, byte View), Func<ushort, ByteReader, GameObjectUpdate, bool>> deltaFields;
        public override uint Opcode => DeltasPacket.Opcode;

        [Preserve]
        public DeltasPacketHandler(GameReplication replication) : base(DeltasPacket.ReadFrom)
        {
            this.replication = replication;
            deltaFields = new Dictionary<(ReplicatedObjectType, byte), Func<ushort, ByteReader, GameObjectUpdate, bool>>
            {
                [(ReplicatedObjectType.Tangible, 3)] = TangibleView3.ReadDeltaField,
                [(ReplicatedObjectType.Creature, 3)] = CreatureView3.ReadDeltaField,
                [(ReplicatedObjectType.Creature, 4)] = CreatureView4.ReadDeltaField,
                [(ReplicatedObjectType.Creature, 6)] = CreatureView6.ReadDeltaField
            };
        }

        protected override void Handle(DeltasPacket packet)
        {
            if (packet.ObjectType == (uint)ReplicatedObjectType.Player && packet.View == 9)
            {
                if (PlayerView9.TryReadDelta(new ByteReader(packet.Payload), out var commands))
                    replication.UpdateCommands(commands);
                return;
            }

            if (!deltaFields.TryGetValue(((ReplicatedObjectType)packet.ObjectType, packet.View), out var readField))
            {
                Debug.LogWarning("Unsupported delta type 0x" + packet.ObjectType.ToString("X8") + " view " + packet.View);
                return;
            }

            var reader = new ByteReader(packet.Payload);
            int count = reader.U16();
            var update = new GameObjectUpdate();
            for (int i = 0; i < count; i++)
            {
                ushort field = reader.U16();
                if (!readField(field, reader, update))
                    throw new InvalidDataException("Unsupported delta field " + field + " for type 0x" + packet.ObjectType.ToString("X8") + " view " + packet.View);
            }

            ReplicationReader.RequireEnd(reader);
            replication.Commit(packet.SwgId, update);
        }
    }
}
