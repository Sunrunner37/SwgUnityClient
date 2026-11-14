using Swg.Client.Core.Networking.Game.Packets.Crafting;
using System.Linq;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Crafting
{
    // Core3/MMOCoreORB/src/server/zone/objects/draftschematic/DraftSchematicImplementation.cpp:76 (DraftSchematicResourceWeights).
    [Preserve]
    internal sealed class DraftSchematicResourceWeightsControllerHandler : ObjectControllerHandler<DraftSchematicResourceWeightsPacket>
    {
        readonly GameReplication replication;
        // High nibble: Core3 ResourceWeight property; low nibble: relative weight.
        static readonly string[] attributes = { "", "Cold resistance", "Conductivity", "Decay resistance", "Heat resistance", "Flavor", "Malleability", "Potential energy", "Overall quality", "Shock resistance", "Unit toughness", "Bulk" };
        public override uint ControllerType => DraftSchematicResourceWeightsPacket.ControllerType;

        [Preserve]
        public DraftSchematicResourceWeightsControllerHandler(GameReplication replication) : base(DraftSchematicResourceWeightsPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(DraftSchematicResourceWeightsPacket packet)
        {
            var weights = packet.Weights.Select(group => group.Weights.Where(value => (value >> 4) != 0 && (value & 15) != 0)
                .Select(value => new DraftSchematicDetails.ExperimentWeight((value >> 4) < attributes.Length ? attributes[value >> 4] : "Resource attribute " + (value >> 4), value & 15)).ToArray()).ToArray();
            replication.UpdatePlayer(state => state.UpdateDraftExperiments(packet.ClientCrc, weights));
        }
    }
}
