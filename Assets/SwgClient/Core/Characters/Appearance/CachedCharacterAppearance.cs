using System.Collections.Generic;

namespace Swg.Client.Core.Characters.Appearance
{
    public sealed class CachedCharacterAppearance
    {
        public string TemplatePath
        {
            get; internal set;
        }
        public PackedCustomization Customization
        {
            get; internal set;
        }
        public string Planet
        {
            get; internal set;
        }
        public IReadOnlyList<CachedWearableAppearance> Wearables
        {
            get; internal set;
        }
        public bool FromCache
        {
            get; internal set;
        }
        public string SourcePath
        {
            get; internal set;
        }
        public string Diagnostic
        {
            get; internal set;
        }
    }
}
