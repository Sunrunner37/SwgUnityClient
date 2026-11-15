using System;
namespace Swg.Client.Core.Crafting
{
    /// <summary>Owner-only crafting permissions and remaining points supplied by PLAY9.</summary>
    public sealed class CraftingProgress
    {
        public int ServerStage { get; internal set; }
        public bool ExperimentationEnabled { get; internal set; }
        public int ExperimentationPoints { get; internal set; }
        public SwgId StationSwgId { get; internal set; }
        public event Action Changed;
        internal void Update(Action<CraftingProgress> apply) { apply(this); Changed?.Invoke(); }
    }
}
