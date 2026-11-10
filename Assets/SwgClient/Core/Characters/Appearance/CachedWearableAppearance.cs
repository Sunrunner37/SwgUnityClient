using Swg.Client.Core.Assets.Formats.Customization;

namespace Swg.Client.Core.Characters.Appearance
{
    public sealed class CachedWearableAppearance
    {
        public string TemplatePath { get; internal set; }
        public PackedCustomization Customization { get; internal set; }
        // The native ITEM record ends with this LE integer (4 in the observed cache).
        // Keep the value; rendering/containment interpretation belongs to the caller.
        public uint ArrangementHint { get; internal set; }
    }
}
