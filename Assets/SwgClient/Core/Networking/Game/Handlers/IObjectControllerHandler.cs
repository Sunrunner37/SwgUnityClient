using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal interface IObjectControllerHandler
    {
        uint ControllerType { get; }

        void Handle(ObjectControllerPacket packet);
    }
}
