using System;

namespace Swg.Client.Core.World.SceneObjects
{
    public sealed class CreatureSceneObject : TangibleSceneObject
    {
        public WorldEquipment[] Equipment = Array.Empty<WorldEquipment>();
        public byte Posture;
        internal override SceneObject Copy()
        {
            var copy = (CreatureSceneObject)base.Copy();
            copy.Equipment = new WorldEquipment[Equipment.Length];
            for (int i = 0; i < Equipment.Length; i++)
            {
                var item = Equipment[i];
                copy.Equipment[i] = new WorldEquipment((byte[])item.Customization.Clone(), item.Arrangement, item.SwgId, item.TemplateCrc);
            }

            return copy;
        }
    }
}
