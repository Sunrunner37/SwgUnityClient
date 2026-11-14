using System.Linq;
using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;
namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    [Preserve]
    internal sealed class DraftSchematicSlotsControllerHandler : ObjectControllerHandler<DraftSchematicSlotsPacket>
    {
        readonly GameReplication replication;
        public override uint ControllerType => DraftSchematicSlotsPacket.ControllerType;
        [Preserve]
        public DraftSchematicSlotsControllerHandler(GameReplication replication) : base(DraftSchematicSlotsPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(DraftSchematicSlotsPacket packet)
        {
            replication.UpdateDraftDetails(packet.ClientCrc, new DraftSchematicDetails
            {
                Complexity = packet.Complexity,
                Size = packet.Size,
                Ingredients = packet.Slots.Select(slot => new DraftSchematicDetails.Ingredient(
                    "@" + slot.Name.File + ":" + slot.Name.Key,
                    slot.ResourceType, slot.Quantity, slot.Optional != 0)).ToArray()
            });
        }
    }
}
