namespace Swg.Client.Core.Characters.Appearance
{
    /// <summary>An appearance loaded for the character list, with its cached location and file provenance.</summary>
    public sealed class CharacterAppearanceProfile
    {
        public CharacterAppearance Appearance { get; internal set; }
        public string Planet { get; internal set; } = "";
        public string SourcePath { get; internal set; } = "";
        public bool FromCache => !string.IsNullOrEmpty(SourcePath);
    }
}
