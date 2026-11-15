using System;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class CellSceneObjectUpdate : SceneObjectUpdate
    {
        public int Number;
        internal override Type RequiredType => typeof(CellSceneObject);
        public override SceneObjectChange Changes => SceneObjectChange.Containment;
        internal override void ApplyTo(SceneObject sceneObject) => ((CellSceneObject)sceneObject).Number = Number;
        public static SceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            reader.Float();
            reader.Ascii(); reader.U32(); reader.Ascii(); reader.Unicode(); reader.U32();
            return new CellSceneObjectUpdate { Number = unchecked((int)reader.U32()) };
        }
    }
}
