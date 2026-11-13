using System;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    /// <summary>The three primary HAM pools and their six secondary attributes, independent of wire ordering.</summary>
    public struct HamPool
    {
        public HamElement Health;
        public HamElement Strength;
        public HamElement Constitution;
        public HamElement Action;
        public HamElement Quickness;
        public HamElement Stamina;
        public HamElement Mind;
        public HamElement Focus;
        public HamElement Willpower;

        public bool HasAnAttackableElement => Health.Maximum > 0 || Action.Maximum > 0 || Mind.Maximum > 0;
        public int LargestMaximum => Math.Max(Health.Maximum, Math.Max(Action.Maximum, Mind.Maximum));
    }
}
