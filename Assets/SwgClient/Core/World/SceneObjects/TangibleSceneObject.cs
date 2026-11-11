using System;

namespace Swg.Client.Core.World.SceneObjects
{
    public class TangibleSceneObject : SpatialSceneObject
    {
        public byte[] Customization = Array.Empty<byte>();
        public string AlternateAppearance = "";
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is TangibleSceneObject tangible)
            {
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
