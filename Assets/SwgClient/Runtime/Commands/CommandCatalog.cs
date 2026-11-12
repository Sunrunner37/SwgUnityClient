using Swg.Client.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Swg.Client.Core.World;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.Commands
{
    public sealed class CommandCatalog : IDisposable
    {
        public sealed class Command
        {
            public string Name;
            public string Arguments = "";
            public string Key => Arguments.Length == 0 ? Name : Name + "+" + Arguments;
            public string Label;
            public string Category;
            public bool BrowserVisible;
            public string ClientHook = "";
            internal string Ability;
            internal bool AlwaysAvailable;
        }

        readonly List<Command> definitions = new List<Command>();
        readonly List<Command> available = new List<Command>();
        readonly PlayerCommands player;
        public event Action Changed;
        public IReadOnlyList<Command> Commands => available;
        internal IReadOnlyList<Command> Definitions => definitions;
        public Task Loading { get; }

        public CommandCatalog(TreRuntimeAssets assets, WorldState state)
        {
            player = state.Commands;
            player.Changed += Refresh;
            Loading = LoadAsync(assets);
        }

        internal CommandCatalog(IEnumerable<Command> definitions, PlayerCommands player)
        {
            this.player = player;
            this.definitions.AddRange(definitions);
            player.Changed += Refresh;
            Loading = Task.CompletedTask;
            Refresh();
        }

        sealed class Metadata
        {
            public List<Command> Commands = new List<Command>();
            public Dictionary<string, string> Names;
        }

        async Task LoadAsync(TreRuntimeAssets assets)
        {
            var metadata = await assets.ReadAsync(loader =>
            {
                var files = loader.Catalog();
                var data = new Metadata
                {
                    Names = files.ContainsKey("string/en/cmd_n.stf") ? new Dictionary<string, string>(StfStringTable.Parse(loader.Read("string/en/cmd_n.stf")), StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>()
                };
                // Native browser lists visibility >= 2; only values > 2 are always available.
                // SWG-Source/client-tools: SwgCuiCommandBrowser::isCommandForPage / appendCommandTableCommands.
                var combatGroups = new HashSet<uint>(new[] { "combat_ranged", "combat_melee", "combat_general", "combat_non_loop" }.Select(ObjectTemplateNames.TemplateCrc));
                foreach (string path in new[]
                {
                    "datatables/command/client_command_table.iff",
                    "datatables/command/client_command_table_ground.iff",
                    "datatables/command/command_table.iff",
                    "datatables/command/command_table_ground.iff"
                })
                {
                    bool clientTable = path.Contains("/client_");
                    foreach (var row in DataTableFile.Parse(loader.Read(path)).Rows)
                    {
                        if (row.Get<string>("commandName").Length == 0 || row.Get<bool>("disabled") || row.Get<int>("godLevel") > 0)
                            continue;
                        data.Commands.Add(new Command
                        {
                            Name = row.Get<string>("commandName"),
                            Ability = row.Get<string>("characterAbility"),
                            AlwaysAvailable = row.Get<int>("visible") > 2,
                            BrowserVisible = row.Get<int>("visible") >= 2,
                            Category = combatGroups.Contains(row.Get<uint>("commandGroup")) ? "combat" : "other",
                            ClientHook = clientTable ? row.Get<string>("cppHook") : ""
                        });
                    }
                }

                return data;
            });
            foreach (var group in metadata.Commands.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                var command = group.OrderByDescending(c => c.AlwaysAvailable).ThenByDescending(c => c.BrowserVisible).First();
                command.ClientHook = group.Select(c => c.ClientHook).FirstOrDefault(hook => hook.Length > 0) ?? "";
                string readable = Regex.Replace(command.Name, "([a-z])([A-Z])", "$1 $2");
                command.Label = metadata.Names.TryGetValue(command.Name, out var label) ? label : char.ToUpperInvariant(readable[0]) + readable.Substring(1);
                definitions.Add(command);
            }
            Refresh();
        }

        void Refresh()
        {
            var owned = new HashSet<string>(player.Abilities, StringComparer.OrdinalIgnoreCase);
            available.Clear();
            available.AddRange(definitions.Where(c => c.AlwaysAvailable || owned.Contains(c.Name) || c.Ability != "" && owned.Contains(c.Ability)));
            foreach (string ability in owned)
            {
                int separator = ability.IndexOf('+');
                if (separator < 1)
                    continue;
                string name = ability.Substring(0, separator);
                var template = definitions.Find(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                if (template == null)
                    continue;
                string arguments = ability.Substring(separator + 1);
                available.Add(new Command { Name = template.Name, Arguments = arguments, Label = template.Label + " (" + arguments + ")", Category = template.Category, BrowserVisible = template.BrowserVisible, ClientHook = template.ClientHook, Ability = ability });
            }

            available.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Label, b.Label));
            Changed?.Invoke();
        }

        public Command Find(string name) => available.Find(c => string.Equals(c.Key, name, StringComparison.OrdinalIgnoreCase));
        // Parameterized grants (e.g. flourish+1) authorize only that argument variant.
        public bool TryResolve(string name, string arguments, out Command command)
        {
            command = available.Find(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                && (c.Arguments.Length == 0 || string.Equals(c.Arguments, arguments, StringComparison.OrdinalIgnoreCase)
                    || arguments.StartsWith(c.Arguments + " ", StringComparison.OrdinalIgnoreCase)));
            return command != null;
        }

        public void Dispose() => player.Changed -= Refresh;
    }
}

