using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Networking.Game.Packets.Player;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Character-sheet requests and native display metadata. Replicated values remain in shared world state.</summary>
    public sealed class ClientCharacterSheet : IDisposable
    {
        public static readonly string[] AttributeNames = { "Health", "Strength", "Constitution", "Action", "Quickness", "Stamina", "Mind", "Focus", "Willpower" };
        readonly WorldState state;
        readonly Action<string, string, SwgId> send;
        readonly Action<FactionRequestPacket> requestFaction;
        readonly StringTableLocalizer strings;
        CharacterSheetData observedData;
        bool migrationPending;
        bool refreshMigrationAfterPending;
        DataTableFile limits;
        (int Index, string Key, bool Show)[] badges;
        public Task Loading { get; }
        public CharacterSheetData Data => state.CharacterSheet;
        public CreatureSceneObject Player => state.SceneObjects.TryGet<CreatureSceneObject>(state.PlayerSwgId, out var player) ? player : null;
        public string SpeciesId => Path.GetFileNameWithoutExtension(Player?.TemplatePath ?? "").Replace("shared_", "").Replace("_female", "").Replace("_male", "");
        public string SpeciesName => Label("species", SpeciesId);

        public ClientCharacterSheet(WorldState state, TreRuntimeAssets assets, StringTableLocalizer strings, Action<string, string, SwgId> send, Action<FactionRequestPacket> requestFaction)
        {
            this.state = state;
            this.send = send;
            this.requestFaction = requestFaction;
            this.strings = strings;
            state.SceneStarted += SceneStarted;
            SceneStarted();
            Loading = Load(assets);
        }
        async Task Load(TreRuntimeAssets assets)
        {
            var loaded = await assets.ReadAsync(loader => (
                Limits: DataTableFile.Parse(loader.Read("datatables/creation/attribute_limits.iff")),
                Badges: DataTableFile.Parse(loader.Read("datatables/badge/badge_map.iff"))));
            limits = loaded.Limits;
            badges = loaded.Badges.Rows.Select(row => (row.Get<int>("INDEX"), row.Get<string>("KEY"), row.Get<int>("SHOW") != 0)).ToArray();
        }
        public void Open()
        {
            send("requestCharacterSheetInfo", "", state.PlayerSwgId);
            send("requestBadges", "", state.PlayerSwgId);
            send("requestBiography", "", state.PlayerSwgId);
            requestFaction(new FactionRequestPacket());
        }
        public void RequestMigration()
        {
            Data.Update(data => data.HasMigrationTargets = false, CharacterSheetChange.Migration);
            // The native reply has no request ID. Keep one request in flight and discard
            // its reply if a newer popup opening needs a fresh request afterward.
            if (migrationPending)
            {
                refreshMigrationAfterPending = true;
                return;
            }
            migrationPending = true;
            send("requestStatMigrationData", "", state.PlayerSwgId);
        }
        void MigrationReceived(CharacterSheetChange changes)
        {
            if ((changes & CharacterSheetChange.Migration) == 0 || !Data.HasMigrationTargets) return;
            migrationPending = false;
            if (!refreshMigrationAfterPending) return;
            refreshMigrationAfterPending = false;
            RequestMigration();
        }
        void SceneStarted()
        {
            if (observedData != null) observedData.Changed -= MigrationReceived;
            observedData = Data;
            observedData.Changed += MigrationReceived;
            migrationPending = false;
            refreshMigrationAfterPending = false;
        }
        public void Dispose()
        {
            state.SceneStarted -= SceneStarted;
            observedData.Changed -= MigrationReceived;
        }
        public void CancelMigration() => send("requestStatMigrationStop", "", state.PlayerSwgId);
        public bool SaveMigration(HamAttributes targets, out string error)
        {
            error = null;
            if (!Data.HasMigrationTargets || targets.Total != Data.MigrationBudget) error = "Assign all available points before saving.";
            var minimum = MigrationLimits(false);
            var maximum = MigrationLimits(true);
            for (int i = 0; i < 9; i++)
                if (targets[i] < minimum[i] || targets[i] > maximum[i]) error = "An attribute is outside your species limits.";
            if (error != null) return false;
            send("requestSetStatMigrationData", string.Join(" ", targets.ToArray()), state.PlayerSwgId);
            return true;
        }
        public HamAttributes MigrationLimits(bool maximum)
        {
            var row = limits.Rows.FirstOrDefault(row => row.Get<string>("male_template") == SpeciesId + "_male")
                ?? limits.Rows.First(row => row.Get<string>("male_template") == "human_male");
            var result = new HamAttributes();
            for (int i = 0; i < 9; i++) result[i] = row.Get<int>((maximum ? "max_" : "min_") + AttributeNames[i].ToLowerInvariant());
            return result;
        }
        public string Planet(string planet) => string.IsNullOrEmpty(planet) ? "Unknown" : Label("planet_n", planet);
        string Label(string file, string key)
        {
            string value = strings.Lookup(file, key);
            return value.StartsWith("@", StringComparison.Ordinal) ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key.Replace('_', ' ')) : value;
        }
        public string BadgesText()
        {
            var owned = new StringBuilder("Badges you have won:\n");
            var missing = new StringBuilder("Badges you have not yet won:\n");
            foreach (var badge in badges.Where(badge => badge.Show))
            {
                string caption = Label("badge_n", badge.Key);
                if (Data.HasBadge(badge.Index)) owned.Append("<color=#BFFF00>  *  ").Append(caption).AppendLine("</color>");
                else missing.Append("    ...").AppendLine(caption);
            }
            return owned.Append(missing).ToString();
        }
        public static HamElement[] Attributes(HamPool ham) => new[] { ham.Health, ham.Strength, ham.Constitution, ham.Action, ham.Quickness, ham.Stamina, ham.Mind, ham.Focus, ham.Willpower };
    }
}
