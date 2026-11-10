using UnityEngine;
using Autofac;
using System;
using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Handlers;

namespace Swg.Client.Core.Networking.Game
{
    public sealed class GamePacketRouter
    {
        readonly Dictionary<uint, IGamePacketHandler> routes;
        public GamePacketRouter(ILifetimeScope services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            routes = PacketHandlerDiscovery.Map(PacketHandlerDiscovery.Create<IGamePacketHandler>(services), handler => handler.Opcode);
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
