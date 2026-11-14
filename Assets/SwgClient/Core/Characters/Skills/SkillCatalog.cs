using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Characters.Skills
{
    /// <summary>Native profession hierarchy and skill requirements from datatables/skill/skills.iff.</summary>
    public sealed class SkillCatalog
    {
        public sealed class Skill
        {
            public string Name;
            public string Label;
            public string Parent;
            public bool IsProfession;
            public bool Hidden;
            public int Graph;
            public int Points;
            public string ExperienceType;
            public int ExperienceCost;
            public string[] Required;
            public string[] Preclusions;
            public string[] Commands;
            public string[] SchematicGroups;
            public Dictionary<string, int> Modifiers;
        }
        /// <summary>Static profession roles and branch progression, shared by every view of the catalog.</summary>
        public sealed class ProfessionTree
        {
            public IReadOnlyList<Skill> Skills;
            public Skill Novice;
            public Skill Master;
            public IReadOnlyList<Skill[]> Branches;
            public IReadOnlyList<Skill[]> Destinations;
        }
        readonly Dictionary<Skill, ProfessionTree> trees = new Dictionary<Skill, ProfessionTree>();
        readonly Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
        readonly Dictionary<string, string[]> schematicGroups = new Dictionary<string, string[]>();
        readonly Dictionary<string, IReadOnlyDictionary<string, string>> labels = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        public IReadOnlyDictionary<string, Skill> Skills => skills;
        public IReadOnlyList<Skill> Professions { get; private set; }
        public const int MaximumSkillPoints = 250; // Core3 SkillManager's character skill-point budget.

        public static SkillCatalog Load(TreAssetLoader loader)
        {
            var catalog = new SkillCatalog();
            foreach (string table in new[] { "skl_n", "exp_n", "stat_n", "cmd_n" })
                catalog.labels.Add(table, StfStringTable.Parse(loader.Read("string/en/" + table + ".stf")));
            foreach (var row in DataTableFile.Parse(loader.Read("datatables/skill/skills.iff")).Rows)
            {
                var skill = new Skill
                {
                    Name = row.Get<string>("NAME"), Parent = row.Get<string>("PARENT"),
                    IsProfession = row.Get<bool>("IS_PROFESSION"), Hidden = row.Get<bool>("IS_HIDDEN") || row.Get<bool>("GOD_ONLY"),
                    Graph = row.Get<int>("GRAPH_TYPE"), Points = row.Get<int>("POINTS_REQUIRED"),
                    ExperienceType = row.Get<string>("XP_TYPE"), ExperienceCost = row.Get<int>("XP_COST"),
                    Required = Split(row.Get<string>("SKILLS_REQUIRED")), Preclusions = Split(row.Get<string>("PRECLUSION_SKILLS")),
                    Commands = Split(row.Get<string>("COMMANDS")), SchematicGroups = Split(row.Get<string>("SCHEMATICS_GRANTED")),
                    Modifiers = ParseModifiers(row.Get<string>("SKILL_MODS"))
                };
                skill.Label = catalog.Label("skl_n", skill.Name);
                catalog.skills.Add(skill.Name, skill);
            }
            catalog.Professions = catalog.skills.Values.Where(skill => skill.IsProfession && !skill.Hidden && skill.Graph == 4)
                .OrderBy(skill => !catalog.IsBasic(skill)).ThenBy(skill => skill.Label, StringComparer.CurrentCulture).ToArray();
            foreach (var profession in catalog.Professions)
                catalog.trees.Add(profession, catalog.BuildTree(profession));
            foreach (var group in DataTableFile.Parse(loader.Read("datatables/crafting/schematic_group.iff")).Rows.GroupBy(row => row.Get<string>("GroupId")))
                catalog.schematicGroups.Add(group.Key, group.Select(row => row.Get<string>("SchematicName")).Distinct().ToArray());
            return catalog;
        }
        static Dictionary<string, int> ParseModifiers(string value)
        {
            var modifiers = new Dictionary<string, int>();
            foreach (string entry in Split(value))
            {
                int separator = entry.IndexOf('=');
                if (separator >= 0) modifiers[entry.Substring(0, separator)] = ModifierValue(entry.Substring(separator + 1));
            }
            return modifiers;
        }
        static int ModifierValue(string value)
        {
            // The shipped Commando row has a missing comma. Match Core3 Integer::valueOf's numeric-prefix behavior.
            return int.Parse(Regex.Match(value, @"^[+-]?\d+").Value, CultureInfo.InvariantCulture);
        }
        static string[] Split(string value) => value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToArray();
        public string Label(string table, string key) => labels[table].TryGetValue(key, out var value) ? value : Readable(key);
        static string Readable(string value) => string.Join(" ", value.Replace("cert_", "").Split('_').Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word.Substring(1)));
        public Skill Novice(Skill profession) => skills.TryGetValue(profession.Name + "_novice", out var novice) ? novice : null;
        public bool IsBasic(Skill profession) => Novice(profession)?.Required.Length == 0;
        public Skill Profession(Skill skill)
        {
            while (!skill.IsProfession && skills.TryGetValue(skill.Parent, out var parent)) skill = parent;
            return skill.IsProfession ? skill : null;
        }
        public ProfessionTree Tree(Skill profession) => trees[profession];
        ProfessionTree BuildTree(Skill profession)
        {
            var members = skills.Values.Where(skill => !skill.IsProfession && !skill.Hidden && Profession(skill) == profession).ToArray();
            var firsts = members.Where(skill => skill.Name.EndsWith("_01", StringComparison.Ordinal)).ToArray();
            var branches = new Skill[4][];
            var destinations = new Skill[4][];
            for (int column = 0; column < branches.Length; column++)
            {
                string prefix = column < firsts.Length ? firsts[column].Name.Substring(0, firsts[column].Name.Length - 2) : "";
                branches[column] = Enumerable.Range(1, 4).Select(tier => members.FirstOrDefault(skill => skill.Name == prefix + tier.ToString("00"))).ToArray();
                var top = branches[column][3];
                destinations[column] = top == null ? Array.Empty<Skill>() : Professions.Where(item => Novice(item)?.Required.Contains(top.Name) == true).ToArray();
            }
            return new ProfessionTree
            {
                Skills = members,
                Novice = Novice(profession),
                Master = members.FirstOrDefault(skill => skill.Name.EndsWith("_master", StringComparison.Ordinal)),
                Branches = branches,
                Destinations = destinations
            };
        }
        public IEnumerable<string> Schematics(Skill skill) => skill.SchematicGroups.Where(schematicGroups.ContainsKey).SelectMany(group => schematicGroups[group]).Distinct();
        public bool HasPrerequisites(Skill skill, ICollection<string> learned) => skill.Required.All(learned.Contains) && !skill.Preclusions.Any(learned.Contains);
        public int PointsAvailable(IEnumerable<string> learned) => Math.Max(0, MaximumSkillPoints - learned.Distinct().Where(skills.ContainsKey).Sum(name => skills[name].Points));
    }
}
