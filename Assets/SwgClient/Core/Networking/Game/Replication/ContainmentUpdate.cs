namespace Swg.Client.Core.Networking.Game.Replication
{
    internal readonly struct ContainmentUpdate
    {
        public readonly SwgId ParentSwgId;
        public readonly uint? Arrangement;
        public ContainmentUpdate(SwgId parentSwgId, uint? arrangement = null)
        {
            ParentSwgId = parentSwgId;
            Arrangement = arrangement;
        }
    }
}
