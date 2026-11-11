using Swg.Client.Core.Assets.Formats.Customization;
using System.Collections.Generic;

namespace Swg.Client.Core.Characters.Appearance
{
    public sealed class CharacterAppearance
    {
        public string TemplatePath { get; internal set; }
        public PackedCustomization Customization { get; internal set; }
        public IReadOnlyList<WearableAppearance> Wearables { get; internal set; }
    }
}
