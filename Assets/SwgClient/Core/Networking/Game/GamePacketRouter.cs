using UnityEngine;
using System;
using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Handlers;

namespace Swg.Client.Core.Networking.Game
{
    public sealed class GamePacketRouter
    {
        readonly Dictionary<uint, IGamePacketHandler> routes;
        public GamePacketRouter(GameWorldState world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            routes = PacketHandlerDiscovery.Map(PacketHandlerDiscovery.Create<IGamePacketHandler>(world), handler => handler.Opcode);
        }

        public void Accept(byte[] packet)
        {
            try
            {
                uint opcode = ByteReader.At(packet, sizeof(ushort));
                if (routes.TryGetValue(opcode, out var handle))
                    handle.Handle(new ByteReader(packet));
                else
                    Debug.LogWarning("Unsupported game opcode 0x" + opcode.ToString("X8"));
            }
            catch (NotImplementedException error)
            {
                Debug.LogWarning("Unimplemented server action: " + error.Message);
            }
            catch (Exception error)
            {
                Debug.LogError("Error when processing game packet: " + error.Message);
            }
        }
    }
}
