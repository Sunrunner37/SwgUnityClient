using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal interface IGamePacketHandler
    {
        uint Opcode { get; }

        void Handle(ByteReader reader);
    }
}
