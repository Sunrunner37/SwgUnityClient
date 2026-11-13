using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using System;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    public sealed class CreatureSceneObject : TangibleSceneObject
    {
        public WorldEquipment[] Equipment = Array.Empty<WorldEquipment>();
        public CreaturePosture Posture;
        public SwgId TargetSwgId;
        public HamPool Ham;
        public float RunSpeed;
        public float SpeedMultiplier = 1;
        public float TurnScale = 1;
        // Matches Core3 PlayerManager's allowed speed: getRunSpeed() * getSpeedMultiplierMod().
        public float RunningSpeed => Math.Max(0, RunSpeed * SpeedMultiplier);

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
