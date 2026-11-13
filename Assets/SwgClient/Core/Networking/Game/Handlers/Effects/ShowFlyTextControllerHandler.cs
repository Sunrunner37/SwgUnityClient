using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    [Preserve]
    internal sealed class ShowFlyTextControllerHandler : ObjectControllerHandler<ShowFlyTextPacket>
    {
        readonly ClientFlyText flyText;
        public override uint ControllerType => ShowFlyTextPacket.ControllerType;
        [Preserve]
        public ShowFlyTextControllerHandler(ClientFlyText flyText) : base(ShowFlyTextPacket.ReadFrom) => this.flyText = flyText;
        protected override void Handle(ShowFlyTextPacket packet) => flyText.Receive(packet);
    }
}
