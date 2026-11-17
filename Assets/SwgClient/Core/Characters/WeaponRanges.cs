using System;
using Swg.Client.Core;

namespace Swg.Client.Core.Characters
{
    /// <summary>Server-reported range and accuracy points for the local player's equipped weapon.</summary>
    public readonly struct WeaponRanges
    {
        public readonly SwgId WeaponSwgId;
        public readonly float IdealRange;
        public readonly float MaximumRange;
        public readonly int PointBlankAccuracy;
        public readonly int IdealAccuracy;
        public readonly int MaximumRangeAccuracy;

        /// <summary>Interpolates the server's weapon accuracy points; false means unknown or outside weapon range.</summary>
        public bool TryAccuracyAt(float distance, out int modifier)
        {
            modifier = 0;
            if (MaximumRange <= 0 || distance > MaximumRange) return false;
            if (distance <= 0) { modifier = PointBlankAccuracy; return true; }
            if (distance >= MaximumRange) { modifier = MaximumRangeAccuracy; return true; }
            // WeaponRanges has no point-blank distance field: the client curve starts at zero.
            float startRange = 0;
            float endRange = IdealRange;
            int startAccuracy = PointBlankAccuracy;
            int endAccuracy = IdealAccuracy;
            if (distance > IdealRange)
            {
                startRange = IdealRange; endRange = MaximumRange;
                startAccuracy = IdealAccuracy; endAccuracy = MaximumRangeAccuracy;
            }
            float accuracy = endRange == startRange ? IdealAccuracy
                : startAccuracy + Math.Max(0, distance - startRange) / (endRange - startRange) * (endAccuracy - startAccuracy);
            modifier = (int)Math.Round(accuracy, MidpointRounding.AwayFromZero);
            return true;
        }

        /// <summary>Standing, stationary overhead estimate, expressed relative to the client's neutral 50% hit chance.</summary>
        public bool TryAttackModifierAt(float distance, out int modifier)
        {
            modifier = 0;
            if (!TryAccuracyAt(distance, out int rangeAccuracy)) return false;
            // Original SWGEmu.exe: 0x10a9560 starts at 0.35; 0xfd66f8 displays
            // (hitChance - 0.5) * 100. These are client UI estimates, not server hit rolls.
            const int baseHitChancePercent = 35;
            const int neutralHitChancePercent = 50;
            int hitChance = Math.Max(0, Math.Min(100, baseHitChancePercent + rangeAccuracy));
            modifier = hitChance - neutralHitChancePercent;
            return true;
        }

        public WeaponRanges(SwgId weaponSwgId, float idealRange, float maximumRange, int pointBlankAccuracy, int idealAccuracy, int maximumRangeAccuracy)
        {
            WeaponSwgId = weaponSwgId;
            IdealRange = idealRange;
            MaximumRange = maximumRange;
            PointBlankAccuracy = pointBlankAccuracy;
            IdealAccuracy = idealAccuracy;
            MaximumRangeAccuracy = maximumRangeAccuracy;
        }
    }
}
