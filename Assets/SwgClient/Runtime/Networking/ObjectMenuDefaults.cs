using System;
using System.Collections.Generic;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>
    /// Supplies base entries Core3 expects before adding its own entries to ObjectMenuRequest.
    /// Identifiers come from RadialOptions.h and SceneObjectType.h; types come from SHOT metadata.
    /// Includes server-radial actions, local Examine, and the native crafting Use action.
    /// </summary>
    public static class ObjectMenuDefaults
    {
        public const byte VehicleEnterExitAction = 205;
        public const byte ExamineAction = 7;
        public const byte UseAction = 20;
        public const byte MissionListAction = 33;
        public const byte MissionDetailsAction = 34;
        public const byte MissionAbortAction = 62;
        public static bool IsVehicle(int objectType) => (objectType & ~0xff) == 0x10000;
        public static bool IsMissionTerminal(int objectType) => objectType == 0x4006;
        public const byte LootAction = 35;
        public const byte LootAllAction = 36;
        public const byte PetCallAction = 44;
        public const byte PetStoreAction = 59;
        public const byte VehicleGenerateAction = 60;
        public const byte VehicleStoreAction = 61;
        const int PetControlDevice = 0x807;
        const int VehicleControlDevice = 0x808;
        const int DroidControlDevice = 0x80b;
        const int BankTerminal = 0x4001;

        public static bool IsCrafting(int objectType) => objectType == 0x8001 || objectType == 0x2006;

        public static bool IsControlDevice(int objectType) => objectType == PetControlDevice || objectType == DroidControlDevice || objectType == VehicleControlDevice;

        public static RadialMenuEntry[] Create(SceneObject sceneObject, int objectType)
        {
            var entries = new List<RadialMenuEntry> { new RadialMenuEntry(1, 0, ExamineAction, 1, "@ui_radial:examine") };
            if (TryGetDefaultAction(sceneObject, objectType, out byte action))
                entries.Add(new RadialMenuEntry(2, 0, action, objectType == BankTerminal || IsCrafting(objectType) ? (byte)1 : (byte)3, CaptionReference(action)));
            // CreatureObject/AiAgent handle these without adding them to the response.
            if ((objectType & ~0xff) == 0x400 && sceneObject is CreatureSceneObject creature && creature.Posture == CreaturePosture.Dead)
            {
                entries.Add(new RadialMenuEntry((byte)(entries.Count + 1), 0, LootAction, 3, "@ui_radial:loot"));
                entries.Add(new RadialMenuEntry((byte)(entries.Count + 1), 0, LootAllAction, 3, "@ui_radial:loot_all"));
            }
            if (sceneObject is MissionSceneObject && sceneObject.Parent?.TemplatePath?.StartsWith("object/tangible/datapad/", StringComparison.Ordinal) == true)
                entries.Add(new RadialMenuEntry((byte)(entries.Count + 1), 0, MissionAbortAction, 1, "@ui_radial:mission_abort"));
            return entries.ToArray();
        }

        public static bool TryGetDefaultAction(SceneObject sceneObject, int objectType, out byte action)
        {
            switch (objectType)
            {
                case 0x4006: action = MissionListAction; return true;
                case 0x803: action = MissionDetailsAction; return true;
                // VehicleControlDevice.idl and PetControlDeviceImplementation.cpp handle selection only.
                case VehicleControlDevice:
                    action = sceneObject is IntangibleSceneObject vehicle && vehicle.IsActive ? VehicleStoreAction : VehicleGenerateAction;
                    return true;
                case PetControlDevice:
                case DroidControlDevice:
                    action = sceneObject is IntangibleSceneObject pet && pet.IsActive ? PetStoreAction : PetCallAction;
                    return true;
                // BankTerminalMenuComponent attaches server options to this client-provided root.
                case 0x8001: // CraftingTool: native requestCraftingSession command.
                case 0x2006: // CraftingStation: server chooses a compatible inventory tool.
                case BankTerminal:
                // These handlers implement Use but do not supply that entry themselves.
                case 0x200a: // Consumable: food/spice
                case 0x2018: // Consumable: drink
                case 0x200d: // Pharmaceutical subclasses
                case 0x201b: // Pet medicine
                case 0x2019: // FireworkMenuComponent
                case 0x201c: // FireworkShowMenuComponent
                case 0x4011: // TicketCollector
                case 0x2012: // TicketObject
                case 0x2014: // TrapMenuComponent
                case 0x4003: // CloningTerminalMenuComponent
                case 0x4004: // InsuranceTerminalMenuComponent
                case 0x400a: // Native travel terminal type
                case 0x4012: // Core3 travel terminal type
                case 0x8004: // CampKitMenuComponent
                case 0x20003: // ThrowGrenadeMenuComponent
                case 0x20004: // FireHeavyWeaponMenuComponent
                case 0x20006: // FireHeavyWeaponMenuComponent
                case 0x800006: // ResourceDeed
                case 0x800008: // VetHarvesterDeed
                case 0x200f: // SignObject
                case 0x8002: // SurveyTool (only its range/options submenu is server-provided)
                case 0x8003: // RepairTool
                case 0x800001: // Building deed: StructureDeed
                case 0x800002: // Installation deed: StructureDeed
                    action = UseAction;
                    return true;
                default:
                    if (HasTemplateUseAction(sceneObject?.TemplatePath))
                    {
                        action = UseAction;
                        return true;
                    }
                    action = 0;
                    return false;
            }
        }

        // Core3 assigns these Use handlers to otherwise generic shared templates. Their
        // server component names are not transmitted; keep the exceptions together here.
        static readonly HashSet<string> useTemplates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "object/tangible/terminal/shared_terminal_character_builder.iff",
            "object/tangible/item/shared_droid_customization.iff",
            "object/tangible/item/shared_vehicle_customization.iff",
            "object/tangible/component/droid/shared_droid_interplanetary_survey.iff",
            "object/tangible/loot/tool/shared_usable_datapad.iff",
            "object/tangible/loot/dungeon/death_watch_bunker/shared_art_crate.iff",
            "object/tangible/harvesting/shared_shellfish_harvester.iff",
            "object/tangible/theme_park/alderaan/act3/shared_dead_eye_prototype.iff",
            "object/tangible/slicing/shared_slicing_laser_knife.iff"
        };
        static bool HasTemplateUseAction(string path) => path != null &&
            (useTemplates.Contains(path) ||
             path.StartsWith("object/tangible/gambling/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("object/tangible/skill_buff/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("object/tangible/jedi/shared_jedi_holocron_", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("object/tangible/jedi/shared_no_drop_jedi_holocron_", StringComparison.OrdinalIgnoreCase));

        public static string CaptionReference(byte action)
        {
            switch (action)
            {
                case PetCallAction: return "@pet/pet_menu:menu_call";
                case PetStoreAction: return "@pet/pet_menu:menu_store";
                case VehicleGenerateAction: return "@pet/pet_menu:menu_generate";
                case VehicleStoreAction: return "@pet/pet_menu:menu_store";
                case MissionListAction: return "@ui_radial:terminal_mission_list";
                case MissionDetailsAction: return "@ui_radial:mission_details";
                case MissionAbortAction: return "@ui_radial:mission_abort";
                case UseAction: return "@ui_radial:item_use";
                default: return "";
            }
        }
    }
}