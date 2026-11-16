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
