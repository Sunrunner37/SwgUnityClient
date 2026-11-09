using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Characters.Creation
{
    public sealed class CharacterCreationData
    {
        public readonly List<Species> Species = new List<Species>();
        public CharacterCreationData(TreAssetLoader loader)
        {
            var root = UiNode.Parse(loader.Text("ui/ui_avatardata.inc"));
            var source = root.Find("SpeciesSelection") ?? throw new InvalidDataException("Missing species selection data");
            if (UiNode.Parse(loader.Text("ui/ui_avatar_creation.inc")).Find("pageSelection") == null)
                throw new InvalidDataException("Missing original creation screen");
            var strings = StfStringTable.Parse(loader.Read("string/en/player_species.stf"));
            foreach (var row in source.Children)
            {
                string Key(string field)
                {
                    string s = row.Get(field);
                    int p = s.IndexOf(':');
                    if (p < 0)
                        throw new InvalidDataException("Invalid species string reference");
                    return s.Substring(p + 1);
                }

                Species.Add(new Species { Id = row.Get("Name"), Name = strings[Key("Text")], Description = strings[Key("description")], Male = row.Get("appearancemale"), Female = row.Get("appearancefemale") });
            }

            if (Species.Count == 0)
                throw new InvalidDataException("Empty species selection data");
        }
    }
}
