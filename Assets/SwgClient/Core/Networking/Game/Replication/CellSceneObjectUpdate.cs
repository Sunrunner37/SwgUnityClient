using System;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class CellSceneObjectUpdate : SceneObjectUpdate
    {
        public int? Number;
        public bool? AllowEntry;
        internal override Type RequiredType => typeof(CellSceneObject);
        public override SceneObjectChange Changes => (Number.HasValue ? SceneObjectChange.Containment : SceneObjectChange.None)
            | (AllowEntry.HasValue ? SceneObjectChange.State : SceneObjectChange.None);
        internal override void ApplyTo(SceneObject sceneObject)
        {
            var cell = (CellSceneObject)sceneObject;
            if (Number.HasValue) cell.Number = Number.Value;
            if (AllowEntry.HasValue) cell.AllowEntry = AllowEntry.Value;
        }
        public static SceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.Float();
            reader.Ascii(); reader.U32(); reader.Ascii(); reader.Unicode(); reader.U32();
            return new CellSceneObjectUpdate { Number = unchecked((int)reader.U32()) };
        }
    }
}
