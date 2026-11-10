using System;
using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Core.Networking.Login
{
    internal sealed class LoginPacketCollector
    {
        LoginClientTokenPacket token;
        LoginEnumClusterPacket clusters;
        LoginClusterStatusPacket status;
        EnumerateCharacterIdPacket characters;
        readonly HashSet<uint> received = new HashSet<uint>();
        public bool Finished
        {
            get
            {
                return received.Count == requiredPackets.Count;
            }
        }

        public LoginResult Result
        {
            get
            {
                return new LoginResult(token, clusters, status, characters);
            }
        }

        readonly Dictionary<uint, Action<ByteReader>> requiredPackets;
        public LoginPacketCollector()
        {
            requiredPackets = new Dictionary<uint, Action<ByteReader>>
            {
                [LoginClientTokenPacket.Opcode] = reader => token = LoginClientTokenPacket.ReadFrom(reader),
                [LoginEnumClusterPacket.Opcode] = reader => clusters = LoginEnumClusterPacket.ReadFrom(reader),
                [LoginClusterStatusPacket.Opcode] = reader => status = LoginClusterStatusPacket.ReadFrom(reader),
                [EnumerateCharacterIdPacket.Opcode] = reader => characters = EnumerateCharacterIdPacket.ReadFrom(reader)
            };
        }

        public void Accept(byte[] bytes)
        {
            uint opcode = ByteReader.At(bytes, sizeof(ushort));
            if (opcode == ErrorPacket.Opcode)
                throw ErrorPacket.ReadFrom(new ByteReader(bytes)).ToException();
            if (requiredPackets.TryGetValue(opcode, out var store))
            {
                store(new ByteReader(bytes));
                received.Add(opcode);
            }
        }
    }
}
