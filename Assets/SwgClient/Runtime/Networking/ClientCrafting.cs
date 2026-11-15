using Swg.Client.Core.Characters;
using System;
using Swg.Client.Runtime.Networking.Chat;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
namespace Swg.Client.Runtime.Networking
{
    /// <summary>A game-owned crafting session. Inventory consumption, outcomes and experimental values are authoritative server updates.</summary>
    public sealed class ClientCrafting : IDisposable
    {
        public enum Page { Closed, Drafts, Ingredients, Summary, Finish, Experiment, Customize }
        public sealed class Choice
        {
            public int Index;
            public uint Tab;
            public DraftSchematicCatalog.Draft Entry;
            public string Category;
        }
        readonly WorldState state;
        readonly CurrentPlayer currentPlayer;
        readonly DraftSchematicCatalog schematics;
        readonly ClientInventory inventory;
        readonly ClientObjectInteractions interactions;
        readonly StringTableLocalizer strings;
        readonly Action<string, string, SwgId> command;
        readonly Action<CraftingAddIngredientPacket> add;
        readonly Action<CraftingRemoveIngredientPacket> remove;
        readonly ClientChat chat;
        SwgId countdownTool;
        readonly Action<CraftingExperimentPacket> experiment;
        readonly Action<CraftingCustomizationPacket> customize;
        readonly HashSet<SwgId> listening = new HashSet<SwgId>();
        ResourceCraftingCatalog resources;
        readonly Dictionary<int, TangibleSceneObject> acceptedMaterials = new Dictionary<int, TangibleSceneObject>();
        TangibleSceneObject pendingMaterial;
        int pendingSlot;
        byte counter;
        public event Action<int> IngredientOperationStarted;
        bool choosingFinish;
        bool creating;
        bool practice;
        public Task Loading { get; }
        public event Action Changed;
        public Page CurrentPage { get; private set; }
        public IReadOnlyList<Choice> Choices { get; private set; } = Array.Empty<Choice>();
        public Choice Selected { get; private set; }
        public SwgId ToolSwgId { get; private set; }
        public SwgId StationSwgId { get; private set; }
        public SwgId SchematicSwgId { get; private set; }
        public SwgId PrototypeSwgId { get; private set; }
        public DraftSlotData[] Slots { get; private set; } = Array.Empty<DraftSlotData>();
        public bool AllowManufacturing { get; private set; }
        public bool Manufacturing { get; private set; }
        public bool Busy { get; private set; }
        public string Result { get; private set; } = "";
        public string Popup { get; private set; }
        public CraftingProgress Progress { get; }
        public ManufactureSchematicSceneObject Schematic => state.SceneObjects.TryGet<ManufactureSchematicSceneObject>(SchematicSwgId, out var value) ? value : null;
        public ClientCrafting(WorldState state, DraftSchematicCatalog schematics, ClientInventory inventory, ClientObjectInteractions interactions, StringTableLocalizer strings,
            TreRuntimeAssets assets, Action<string, string, SwgId> command, Action<CraftingAddIngredientPacket> add,
            Action<CraftingExperimentPacket> experiment, Action<CraftingCustomizationPacket> customize, Action<CraftingRemoveIngredientPacket> remove, ClientChat chat, CurrentPlayer currentPlayer, CraftingProgress progress)
        {
            Progress = progress;
            this.currentPlayer = currentPlayer;
            this.state = state; this.schematics = schematics; this.inventory = inventory; this.interactions = interactions;
            this.strings = strings; this.command = command; this.add = add; this.experiment = experiment; this.customize = customize;
            this.remove = remove; this.chat = chat;
            Loading = Load(assets);
            state.SceneObjects.ObjectChanged += ObjectChanged;
            Progress.Changed += Notify;
            inventory.Changed += Notify;
            interactions.AttributesChanged += AttributesChanged;
            currentPlayer.DraftDetailsChanged += DetailsChanged;
        }
        async Task Load(TreRuntimeAssets assets) => resources = await assets.ReadAsync(ResourceCraftingCatalog.Load);
        void Notify() => Changed?.Invoke();
        void AttributesChanged(SwgId id) { if (CurrentPage != Page.Closed) Notify(); }
        void DetailsChanged(uint crc) { if (Selected?.Entry.SchematicCrc == crc) Notify(); }
        void ObjectChanged(SceneObject item, SceneObjectChange changes)
        {
            if (item.SwgId == countdownTool && item is TangibleSceneObject tool && (changes & SceneObjectChange.Inventory) != 0 && tool.Quantity > 0)
            {
                countdownTool = default;
                chat.ShowScreenMessage("Creating prototype. Please wait " + tool.Quantity + " seconds.");
            }
            if (CurrentPage == Page.Closed) return;
            if (item is ManufactureSchematicSceneObject && item.ParentSwgId == ToolSwgId && listening.Add(item.SwgId))
            {
                SchematicSwgId = item.SwgId;
                command("synchronizedUiListen", "0", item.SwgId);
            }
            if (item.SwgId == SchematicSwgId || item.SwgId == PrototypeSwgId) Notify();
        }
        public void Request(SwgId toolOrStation) => command("requestCraftingSession", "", toolOrStation);
        public void Receive(CraftingSchematicListPacket packet)
        {
            ToolSwgId = packet.ToolSwgId; StationSwgId = packet.StationId;
            SchematicSwgId = default; PrototypeSwgId = default; listening.Clear();
            Choices = packet.Schematics.Select((value, index) => new Choice { Index = index, Tab = value.ToolTab, Entry = schematics.Get(value.ClientCrc) })
                .Where(value => value.Entry != null).ToArray();
            foreach (var choice in Choices)
            {
                string path = choice.Entry.PreviewPath;
                var parts = path.Split('/');
                string typeKey = state.Templates.GameObjectType(path) switch
                {
                    0x20001 => "weapon_melee_misc", 0x20007 => "weapon_melee_1h", 0x20008 => "weapon_melee_2h",
                    0x20009 => "weapon_melee_polearm", 0x2000a => "weapon_ranged_pistol", 0x2000b => "weapon_ranged_carbine",
                    0x2000c => "weapon_ranged_rifle", 0x20005 => "weapon_heavy_mine", 0x40008 => "component_weapon_melee",
                    0x40009 => "component_weapon_ranged", _ => null
                };
                choice.Category = typeKey != null ? strings.Lookup("got_n", typeKey)
                    : parts.Length > 3 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[parts.Length - 2].Replace('_', ' ')) : "Miscellaneous";
            }
            Busy = false; creating = false; choosingFinish = false; Result = ""; Popup = null; CurrentPage = Page.Drafts; Selected = null; Notify();
        }
        public void Select(Choice choice) { Selected = choice; schematics.RequestDetails(choice.Entry.SchematicCrc); Notify(); }
        public void BeginAssembly()
        {
            if (Selected == null || Busy) return;
            Busy = true;
            command("selectDraftSchematic", Selected.Index.ToString(CultureInfo.InvariantCulture), default);
            Notify();
        }
        public void Receive(CraftingIngredientSlotsPacket packet)
        {
            ToolSwgId = packet.ToolSwgId; SchematicSwgId = packet.ManufactureSchematicSwgId; PrototypeSwgId = packet.PrototypeSwgId;
            acceptedMaterials.Clear(); pendingMaterial = null;
            Slots = packet.Slots; AllowManufacturing = packet.AllowFactory != 0;
            Busy = false; CurrentPage = Page.Ingredients;
            foreach (var item in inventory.Items) interactions.RequestExamine(item.SwgId);
            Notify();
        }
        public TangibleSceneObject SlotMaterial(int slot) => Quantity(slot) > 0 && acceptedMaterials.TryGetValue(slot, out var material) ? material : null;
        public float SlotQuality(int slot) => SlotMaterial(slot) is TangibleSceneObject material ? Quality(material) : 0;
        public int Quantity(int slot) => Schematic != null && slot < Schematic.IngredientQuantities.Count ? Schematic.IngredientQuantities[slot].Sum() : 0;
        public bool CanAssemble => Slots.Length > 0 && Slots.Select((slot, index) => slot.Optional != 0 || Quantity(index) >= slot.Quantity).All(value => value);
        public string ResourceName(string resource) => resources.Name(resource);
        public IEnumerable<TangibleSceneObject> Materials => inventory.Items.Where(item => Slots.Any(slot => Qualifies(item, slot)));
        public bool Qualifies(TangibleSceneObject item, DraftSlotData slot)
        {
            if (slot.Type == 4) return resources?.Matches(item.ResourceType, slot.ResourceType) == true;
            string path = item.TemplatePath ?? "";
            return path == slot.ResourceType || path.Replace("shared_", "") == slot.ResourceType;
        }
        public int ResourceValue(TangibleSceneObject item, string attribute)
        {
            var value = interactions.GetAttributes(item.SwgId).FirstOrDefault(value => ResourceAttribute(value.Name) == attribute);
            return int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : 0;
        }
        public float Quality(TangibleSceneObject item)
        {
            if (Selected == null || !schematics.TryGetDetails(Selected.Entry.SchematicCrc, out var details)) return 0;
            var weights = details.Experiments.SelectMany(group => group).ToArray();
            float total = 0;
            float weighted = 0;
            foreach (var weight in weights)
            {
                var attribute = interactions.GetAttributes(item.SwgId).FirstOrDefault(value => ResourceAttribute(value.Name) == weight.Attribute);
                if (float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                { weighted += value * weight.Weight; total += weight.Weight; }
            }
            return total > 0 ? weighted / total / 1000 : 0;
        }
        static string ResourceAttribute(string key) => key switch
        {
            "res_cold_resist" => "Cold resistance", "res_conductivity" => "Conductivity", "res_decay_resist" => "Decay resistance",
            "res_heat_resist" => "Heat resistance", "res_flavor" => "Flavor", "res_malleability" => "Malleability", "res_potential_energy" => "Potential energy",
            "res_quality" => "Overall quality", "res_shock_resistance" => "Shock resistance", "res_toughness" => "Unit toughness", _ => key
        };
        public void Insert(SwgId ingredient, int slot)
        {
            if (Busy || CurrentPage != Page.Ingredients || slot < 0 || slot >= Slots.Length || !state.SceneObjects.TryGet<TangibleSceneObject>(ingredient, out var item)
                || !Qualifies(item, Slots[slot]) || Quantity(slot) >= Slots[slot].Quantity) return;
            pendingMaterial = item; pendingSlot = slot;
            IngredientOperationStarted?.Invoke(slot);
            Busy = true; add(new CraftingAddIngredientPacket(currentPlayer.SwgId, ingredient, slot, ++counter)); Notify();
        }
        public void Remove(int slot)
        {
            if (Busy || CurrentPage != Page.Ingredients || slot < 0 || slot >= Slots.Length || Quantity(slot) == 0 || inventory.ContainerSwgId.IsNone) return;
            pendingMaterial = null; pendingSlot = slot;
            IngredientOperationStarted?.Invoke(slot);
            Busy = true; remove(new CraftingRemoveIngredientPacket(currentPlayer.SwgId, inventory.ContainerSwgId, slot, ++counter)); Notify();
        }
        public void Assemble()
        {
            if (Busy || !CanAssemble) return;
            Busy = true; command("nextCraftingStage", (++counter).ToString(), default); Notify();
        }
        public void Receive(CraftingAssemblyResultPacket packet)
        {
            Busy = false;
            if (creating)
            {
                creating = false;
                if (!Manufacturing) countdownTool = ToolSwgId;
                Busy = true;
                command(Manufacturing ? "createManfSchematic" : "createPrototype", (++counter) + (Manufacturing ? "" : practice ? " 0" : " 1"), default);
            }
            else if (choosingFinish)
            {
                choosingFinish = false;
                Busy = true; command("nextCraftingStage", (++counter).ToString(), default);
            }
            else if (CurrentPage == Page.Ingredients) { Result = Outcome(packet.Result); CurrentPage = Page.Summary; interactions.RequestExamine(PrototypeSwgId); }
            Notify();
        }
        public void ShowFinish()
        {
            if (!Progress.ExperimentationEnabled || StationSwgId.IsNone) ChooseOutput(false);
            else { CurrentPage = Page.Finish; Notify(); }
        }
        public void ShowExperiment() { CurrentPage = Page.Experiment; Notify(); }
        public void DoneExperimenting() { CurrentPage = Page.Finish; Notify(); }
        public void Experiment(IReadOnlyList<(int Row, int Points)> attempts)
        {
            if (Busy || attempts.Count == 0 || attempts.Any(value => value.Points < 1) || attempts.Sum(value => value.Points) > Progress.ExperimentationPoints) return;
            Busy = true; experiment(new CraftingExperimentPacket(currentPlayer.SwgId, ++counter, attempts)); Notify();
        }
        public void Receive(CraftingExperimentationResultPacket packet)
        {
            Busy = false; Popup = "Experimentation: " + Outcome(packet.Result); interactions.RequestExamine(PrototypeSwgId); Notify();
        }
        public void ChooseOutput(bool manufacturing)
        {
            if (Busy) return;
            Manufacturing = manufacturing;
            choosingFinish = Progress.ServerStage == 3;
            Busy = true;
            command("nextCraftingStage", (++counter).ToString(), default); Notify();
        }
        public void Create(string name, bool practiceMode, int runs)
        {
            if (Busy || CurrentPage != Page.Customize) return;
            Busy = true; creating = true; practice = practiceMode;
            customize(new CraftingCustomizationPacket(currentPlayer.SwgId, name, Math.Max(1, Math.Min(1000, runs)))); Notify();
        }
        public void Receive(CraftingResultPacket packet)
        {
            Busy = false;
            if (packet.Message == 0x107 || packet.Message == 0x108)
            {
                // MSCO resource IDs identify spawns, not inventory containers. Retain only
                // the accepted source for its cached attributes; quantities still come from MSCO.
                if (packet.Message == 0x107 && packet.Result == 0 && pendingMaterial != null) acceptedMaterials[pendingSlot] = pendingMaterial;
                if (packet.Message == 0x108 && packet.Result == 0) acceptedMaterials.Remove(pendingSlot);
                pendingMaterial = null;
                if (packet.Result != 0) Popup = "The server could not change that ingredient (error " + packet.Result + ").";
            }
            else if (packet.Message == 0x10a && packet.Result == 1 && !creating) CurrentPage = Page.Customize;
            else if (packet.Message == 0x15a && creating)
            {
                Busy = true;
                command("nextCraftingStage", (++counter).ToString(), default);
            }
            else if (packet.Message == 0x10b) CurrentPage = Page.Closed;
            Notify();
        }
        public void Receive(CloseCraftingWindowPacket packet) { Busy = false; CurrentPage = Page.Closed; Notify(); }
        public void DismissPopup() { Popup = null; Notify(); }
        public void Back() => Request(ToolSwgId);
        public void Cancel() { command("cancelCraftingSession", "", default); CurrentPage = Page.Closed; Busy = false; Notify(); }
        public static string Outcome(uint value) => value switch { 0 => "Amazing success!", 1 => "Great success!", 2 => "Good success", 3 => "Moderate success", 4 => "Success", 5 => "Marginal success", 6 => "OK", 7 => "Barely succeeded", 8 => "Critical failure", _ => "Result " + value };
        public void Dispose()
        {
            state.SceneObjects.ObjectChanged -= ObjectChanged; Progress.Changed -= Notify; inventory.Changed -= Notify;
            interactions.AttributesChanged -= AttributesChanged; currentPlayer.DraftDetailsChanged -= DetailsChanged;
        }
    }
}
