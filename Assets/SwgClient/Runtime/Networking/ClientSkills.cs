using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Runtime.Commands;
using System.IO;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using System.Threading.Tasks;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Characters.Skills;
using Swg.Client.Core;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Combines server-owned player progress with the TRE profession catalog; never trains skills locally.</summary>
    public sealed class ClientSkills
    {
        public readonly struct Unlock
        {
            public readonly string Label;
            public readonly CommandCatalog.Command Command;
            public readonly string Description;
            public Unlock(string label, CommandCatalog.Command command = null, string description = null)
            { Label = label; Command = command; Description = description; }
        }
        readonly CommandCatalog commands;
        readonly Action<string, string, SwgId> send;
        readonly CurrentPlayer currentPlayer;
        readonly TreRuntimeAssets assets;
        readonly StringTableLocalizer strings;
        readonly Dictionary<string, Task<Unlock[]>> unlocks = new Dictionary<string, Task<Unlock[]>>();
        public SkillCatalog Catalog { get; private set; }
        public Task Loading { get; }
        public CreatureSceneObject Player => currentPlayer.Creature;
        public int AvailablePoints => Catalog.PointsAvailable(Player?.Skills ?? Enumerable.Empty<string>());
        public ClientSkills(TreRuntimeAssets assets, StringTableLocalizer strings, CommandCatalog commands, Action<string, string, SwgId> send, CurrentPlayer currentPlayer)
        {
            this.currentPlayer = currentPlayer;
            this.assets = assets;
            this.strings = strings;
            this.commands = commands;
            this.send = send;
            Loading = Load();
        }
        async Task Load() => Catalog = await assets.ReadAsync(SkillCatalog.Load);
        public bool Owns(SkillCatalog.Skill profession) => Player != null && Catalog.Tree(profession).Skills.Any(skill => Player.Skills.Contains(skill.Name));
        public bool Learned(SkillCatalog.Skill skill) => Player?.Skills.Contains(skill.Name) == true;
        public bool Surrender(SkillCatalog.Skill skill)
        {
            if (!Learned(skill)) return false;
            send("surrenderSkill", skill.Name, currentPlayer.SwgId);
            return true;
        }
        public bool IsNext(SkillCatalog.Skill skill) => Player != null && !Learned(skill) && Catalog.HasPrerequisites(skill, Player.Skills);
        public int Experience(SkillCatalog.Skill skill) => currentPlayer.Experience.TryGetValue(skill.ExperienceType, out var value) ? value : 0;
        public float Progress(SkillCatalog.Skill skill) => skill.ExperienceCost > 0 ? Math.Min(1f, (float)Experience(skill) / skill.ExperienceCost) : 1;
        public IEnumerable<string> Certifications => currentPlayer.Commands.Abilities.Where(name => name.StartsWith("cert_", StringComparison.Ordinal)).Select(name => Catalog.Label("cmd_n", name)).OrderBy(name => name);
        public Task<Unlock[]> Unlocks(SkillCatalog.Skill skill)
        {
            if (!unlocks.TryGetValue(skill.Name, out var result)) unlocks.Add(skill.Name, result = LoadUnlocks(skill));
            return result;
        }
        async Task<Unlock[]> LoadUnlocks(SkillCatalog.Skill skill)
        {
            await commands.Loading;
            var schematics = await assets.ReadAsync(loader => Catalog.Schematics(skill).Select(path =>
            {
                int slash = path.LastIndexOf('/') + 1;
                string shared = path.Insert(slash, "shared_");
                string crafted = SharedDraftSchematicTemplate.CraftedTemplate(loader.Read(shared));
                string name = ObjectTemplateNames.DisplayName(loader, crafted ?? shared);
                return name.Length > 0 ? name : Path.GetFileNameWithoutExtension(path).Replace('_', ' ');
            }).ToArray());
            var granted = new List<Unlock>();
            foreach (string ability in skill.Commands.Where(name => !name.StartsWith("private_", StringComparison.Ordinal)))
            {
                int separator = ability.IndexOf('+');
                string name = separator < 0 ? ability : ability.Substring(0, separator);
                string arguments = separator < 0 ? "" : ability.Substring(separator + 1);
                var definitions = commands.Definitions.Where(command => string.Equals(command.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(command.Ability, ability, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (definitions.Length == 0) granted.Add(new Unlock(Catalog.Label("cmd_n", ability)));
                foreach (var definition in definitions)
                {
                    var command = arguments.Length == 0 ? definition : definition.WithArguments(arguments);
                    string help = strings.Lookup("cmd_d", command.Name.ToLowerInvariant());
                    if (help.StartsWith("@", StringComparison.Ordinal)) help = "/" + command.Name;
                    if (arguments.Length > 0) help = "/" + command.Name + " " + arguments + "\n" + help;
                    granted.Add(new Unlock(command.Label, command, help));
                }
            }
            granted.AddRange(schematics.Select(name => new Unlock(strings.Localize(name))));
            return granted.GroupBy(item => item.Command?.Key ?? item.Label).Select(group => group.First()).OrderBy(item => item.Label).ToArray();
        }
    }
}
