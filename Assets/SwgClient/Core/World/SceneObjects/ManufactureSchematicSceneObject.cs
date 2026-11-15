using Swg.Client.Core.Networking.Game.Replication;
using System.Collections.Generic;
using System.Linq;
namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>A stored manufacturing schematic references a draft and consumes datapad space.</summary>
    public sealed class ManufactureSchematicSceneObject : SceneObject
    {
        public uint DraftSchematicCrc;
        public int DataSize = 1;
        public float Complexity;
        public float ExperimentRisk;
        public List<List<SwgId>> IngredientSwgIds { get; private set; } = new List<List<SwgId>>();
        public List<List<int>> IngredientQuantities { get; private set; } = new List<List<int>>();
        public List<string> ExperimentNames { get; private set; } = new List<string>();
        public List<float> ExperimentValues { get; private set; } = new List<float>();
        public List<float> ExperimentMaximums { get; private set; } = new List<float>();
        public Dictionary<string, float> CraftingValues { get; private set; } = new Dictionary<string, float>();
        internal void Apply(ManufactureSchematicSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.Complexity.HasValue) Complexity = update.Complexity.Value;
            foreach (var apply in update.Crafting) apply(this);
            if (update.DraftSchematicCrc.HasValue) DraftSchematicCrc = update.DraftSchematicCrc.Value;
            if (update.DataSize.HasValue) DataSize = update.DataSize.Value;
        }
        void CopyCraftingTo(ManufactureSchematicSceneObject target)
        {
            target.Complexity = Complexity;
            target.ExperimentRisk = ExperimentRisk;
            target.IngredientSwgIds = IngredientSwgIds.Select(list => new List<SwgId>(list)).ToList();
            target.IngredientQuantities = IngredientQuantities.Select(list => new List<int>(list)).ToList();
            target.ExperimentNames = new List<string>(ExperimentNames);
            target.ExperimentValues = new List<float>(ExperimentValues);
            target.ExperimentMaximums = new List<float>(ExperimentMaximums);
            target.CraftingValues = new Dictionary<string, float>(CraftingValues);
        }
        internal override SceneObject Copy()
        {
            var copy = (ManufactureSchematicSceneObject)base.Copy();
            CopyCraftingTo(copy);
            return copy;
        }
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is ManufactureSchematicSceneObject schematic)
            {
                schematic.DraftSchematicCrc = DraftSchematicCrc;
                schematic.DataSize = DataSize;
                CopyCraftingTo(schematic);
            }
        }
    }
}
