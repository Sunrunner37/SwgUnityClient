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
        readonly Dictionary<(ReplicatedObjectType Type, byte View), Func<DeltasPacket, SceneObjectUpdate>> deltaFields;
        public override uint Opcode => DeltasPacket.Opcode;

        [Preserve]
        public DeltasPacketHandler(GameReplication replication) : base(DeltasPacket.ReadFrom)
        {
            this.replication = replication;
            deltaFields = new Dictionary<(ReplicatedObjectType, byte), Func<DeltasPacket, SceneObjectUpdate>>
            {
                [(ReplicatedObjectType.Mission, 3)] = packet => ReadDelta<MissionSceneObjectUpdate>(packet, MissionView3.ReadDeltaField),
                [(ReplicatedObjectType.ManufactureSchematic, 7)] = packet => ManufactureSchematicView7.ReadDeltaPacket(new ByteReader(packet.Payload)),
                [(ReplicatedObjectType.ManufactureSchematic, 6)] = packet => ReadDelta<ManufactureSchematicSceneObjectUpdate>(packet, ManufactureSchematicView.ReadDelta6),
                [(ReplicatedObjectType.ManufactureSchematic, 3)] = packet => ReadDelta<ManufactureSchematicSceneObjectUpdate>(packet, ManufactureSchematicView.ReadDelta3),
                [(ReplicatedObjectType.Intangible, 3)] = packet => ReadDelta<IntangibleSceneObjectUpdate>(packet, IntangibleView3.ReadDeltaField),
                [(ReplicatedObjectType.Creature, 1)] = packet => ReadDelta<CreatureSceneObjectUpdate>(packet, CreatureView1.ReadDeltaField),
                [(ReplicatedObjectType.ResourceContainer, 3)] = packet => ReadDelta<TangibleSceneObjectUpdate>(packet, ResourceContainerView.ReadDelta3),
                [(ReplicatedObjectType.Tangible, 3)] = packet => ReadDelta<TangibleSceneObjectUpdate>(packet, TangibleView3.ReadDeltaField),
                [(ReplicatedObjectType.Creature, 3)] = packet => ReadDelta<CreatureSceneObjectUpdate>(packet, CreatureView3.ReadDeltaField),
                [(ReplicatedObjectType.Creature, 4)] = packet => ReadDelta<CreatureSceneObjectUpdate>(packet, CreatureView4.ReadDeltaField),
                [(ReplicatedObjectType.Creature, 6)] = packet => ReadDelta<CreatureSceneObjectUpdate>(packet, CreatureView6.ReadDeltaField)
            };
        }

        protected override void Handle(DeltasPacket packet)
        {
            if (packet.ObjectType == (uint)ReplicatedObjectType.Player && packet.View == 8)
            {
                replication.UpdatePlayer(PlayerView8.ReadDelta(new ByteReader(packet.Payload)));
                return;
            }

            if (packet.ObjectType == (uint)ReplicatedObjectType.Player && packet.View == 9)
            {
                if (PlayerView9.TryReadDelta(new ByteReader(packet.Payload), out var commands))
                    replication.UpdatePlayer(state =>
                {
                    if (commands.Abilities != null) state.Commands.Apply(commands.Abilities);
                    if (commands.Schematics != null) state.UpdateDraftSchematics(commands.Schematics);
                    commands.Details?.Invoke(state);
                });
                return;
            }

            if (!deltaFields.TryGetValue(((ReplicatedObjectType)packet.ObjectType, packet.View), out var read))
            {
                Debug.LogWarning("Unsupported delta type 0x" + packet.ObjectType.ToString("X8") + " view " + packet.View);
                return;
            }

            replication.Commit(packet.SwgId, read(packet));
        }

        static SceneObjectUpdate ReadDelta<T>(DeltasPacket packet, Func<ushort, ByteReader, T, bool> readField) where T : SceneObjectUpdate, new()
        {
            var reader = new ByteReader(packet.Payload);
            int count = reader.U16();
            var update = new T();
            for (int i = 0; i < count; i++)
            {
                ushort field = reader.U16();
                if (!readField(field, reader, update))
                    throw new InvalidDataException("Unsupported delta field " + field + " for type 0x" + packet.ObjectType.ToString("X8") + " view " + packet.View);
            }
            ReplicationReader.RequireEnd(reader);
            return update;
        }
    }
}
