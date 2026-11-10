using System;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal abstract class ObjectControllerHandler<TPacket> : IObjectControllerHandler where TPacket : struct, IPacket
    {
        readonly Func<ObjectControllerPacket, TPacket> read;
        public abstract uint ControllerType { get; }

        protected ObjectControllerHandler(Func<ObjectControllerPacket, TPacket> read) => this.read = read ?? throw new ArgumentNullException(nameof(read));
        void IObjectControllerHandler.Handle(ObjectControllerPacket packet) => Handle(read(packet));
        protected abstract void Handle(TPacket packet);
    }
}
