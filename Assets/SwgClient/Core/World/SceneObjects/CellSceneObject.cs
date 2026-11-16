namespace Swg.Client.Core.World.SceneObjects
{
    /// <summary>A building room. Number indexes the building's native portal layout; SwgId identifies the server cell.</summary>
    public sealed class CellSceneObject : SpatialSceneObject
    {
        public int Number;
        public bool AllowEntry = true;
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is CellSceneObject cell)
            {
                cell.Number = Number;
                cell.AllowEntry = AllowEntry;
            }
        }
    }
}
