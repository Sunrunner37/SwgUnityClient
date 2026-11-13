using Swg.Client.Core.Networking.Game.Replication;
using System;

namespace Swg.Client.Core.World.SceneObjects
{
    public class TangibleSceneObject : SpatialSceneObject
    {
        public string ResourceType;
        public int Volume = 1;
        public int Quantity = 1;
        public byte[] Customization = Array.Empty<byte>();
        public string AlternateAppearance = "";
        internal void Apply(TangibleSceneObjectUpdate update)
        {
            base.Apply(update);
            if (update.ResourceType != null)
                ResourceType = update.ResourceType;
            if (update.Volume.HasValue)
                Volume = update.Volume.Value;
            if (update.Quantity.HasValue)
                Quantity = update.Quantity.Value;
            if (update.Customization != null)
                Customization = update.Customization;
            if (update.AlternateAppearance != null)
                AlternateAppearance = update.AlternateAppearance;
        }

        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is TangibleSceneObject tangible)
            {
                tangible.ResourceType = ResourceType;
                tangible.Volume = Volume;
                tangible.Quantity = Quantity;
                tangible.Customization = (byte[])Customization.Clone();
                tangible.AlternateAppearance = AlternateAppearance;
            }
        }

        internal override SceneObject Copy()
        {
            var copy = (TangibleSceneObject)base.Copy();
            copy.Customization = (byte[])Customization.Clone();
            return copy;
        }
    }
}
