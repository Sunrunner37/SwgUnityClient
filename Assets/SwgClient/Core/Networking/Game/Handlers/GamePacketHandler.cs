using System;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal abstract class GamePacketHandler<TPacket> : IGamePacketHandler where TPacket : struct, IPacket
    {
        readonly Func<ByteReader, TPacket> read;
        public abstract uint Opcode { get; }

        protected GamePacketHandler(Func<ByteReader, TPacket> read) => this.read = read ?? throw new ArgumentNullException(nameof(read));
        void IGamePacketHandler.Handle(ByteReader reader) => Handle(read(reader));
        protected abstract void Handle(TPacket packet);
    }
}
