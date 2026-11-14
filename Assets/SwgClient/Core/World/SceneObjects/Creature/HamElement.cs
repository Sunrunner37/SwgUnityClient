using System;

namespace Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature
{
    /// <summary>One named creature attribute: its current value, maximum and wounds.</summary>
    public readonly struct HamElement
    {
        public readonly int Current;
        public readonly int Maximum;
        public readonly int Wounds;
        public readonly int Base;
        public readonly int Encumbrance;
        // Maximum already includes armor penalties; add them back to isolate buffs and other modifiers.
        public int Modifiers => Base > 0 ? Maximum - Base + Encumbrance : 0;
        public int AvailableMaximum => Math.Max(0, Maximum - Wounds);
        public float CurrentFraction => Fraction(Math.Min(Current, AvailableMaximum));
        public float CapacityFraction => Fraction(AvailableMaximum);
        public float WoundFraction => Fraction(Wounds);
        public float RelativeWidth(int largestMaximum) => largestMaximum > 0 ? Math.Max(0, Math.Min(1, (float)Maximum / largestMaximum)) : 0;

        public HamElement(int current, int maximum, int wounds, int baseValue = 0, int encumbrance = 0)
        {
            Current = current;
            Maximum = maximum;
            Wounds = wounds;
            Base = baseValue;
            Encumbrance = encumbrance;
        }

        float Fraction(int value) => Maximum > 0 ? Math.Max(0, Math.Min(1, (float)value / Maximum)) : 0;
    }
}
