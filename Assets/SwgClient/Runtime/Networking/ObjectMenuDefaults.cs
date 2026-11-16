using System;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
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
    public sealed class ObjectMenuDefaults
    {
        readonly WorldState state;
        readonly CurrentPlayer currentPlayer;

        public ObjectMenuDefaults(WorldState state, CurrentPlayer currentPlayer)
        {
            this.state = state;
            this.currentPlayer = currentPlayer;
        }

        public ObjectTypeId ObjectType(SceneObject sceneObject) => string.IsNullOrEmpty(sceneObject?.TemplatePath)
            ? ObjectTypeId.Unknown : (ObjectTypeId)state.Templates.GameObjectType(sceneObject.TemplatePath);

        public RadialMenuEntry[] Create(SceneObject sceneObject) => Create(sceneObject, ObjectType(sceneObject));

        internal RadialMenuEntry[] Create(SceneObject sceneObject, ObjectTypeId objectType)
        {
            var entries = new List<RadialMenuEntry> { ObjectMenuAction.Examine.ToEntry(1) };
            if (TryGetDefaultAction(sceneObject, objectType, out ObjectMenuAction action))
                entries.Add(action.ToEntry(2));
            if (CanDestroy(sceneObject))
                entries.Add(ObjectMenuAction.Destroy.ToEntry((byte)(entries.Count + 1)));
            // CreatureObject/AiAgent handle these without adding them to the response.
            if (objectType.IsCreature() && sceneObject is CreatureSceneObject creature && creature.Posture == CreaturePosture.Dead)
            {
                entries.Add(ObjectMenuAction.Loot.ToEntry((byte)(entries.Count + 1)));
                entries.Add(ObjectMenuAction.LootAll.ToEntry((byte)(entries.Count + 1)));
            }
            if (sceneObject is MissionSceneObject && sceneObject.Parent?.TemplatePath?.StartsWith("object/tangible/datapad/", StringComparison.Ordinal) == true)
                entries.Add(ObjectMenuAction.MissionAbort.ToEntry((byte)(entries.Count + 1)));
            return entries.ToArray();
        }

        public bool TryGetDefaultAction(SceneObject sceneObject, out ObjectMenuAction action) => TryGetDefaultAction(sceneObject, ObjectType(sceneObject), out action);

        public bool CanDestroy(SceneObject sceneObject)
        {
            if (!(sceneObject is TangibleSceneObject tangible)) return false;
            // Inventory items include nested bags and equipped wearables/weapons, but not player-owned system slots.
            if (sceneObject.ParentSwgId == currentPlayer.SwgId && ClientInventory.CanEquip(tangible)) return true;
            for (var parent = sceneObject.Parent; parent != null; parent = parent.Parent)
                if (parent.ParentSwgId == currentPlayer.SwgId && parent.TemplatePath?.StartsWith("object/tangible/inventory/", StringComparison.Ordinal) == true)
                    return true;
            return false;
        }

        internal bool TryGetDefaultAction(SceneObject sceneObject, ObjectTypeId objectType, out ObjectMenuAction action)
        {
            if (sceneObject is TangibleSceneObject tangible && tangible.CanConverse)
            {
                action = ObjectMenuAction.ConverseStart;
                return true;
            }
            if (objectType.IsContainer()) { action = ObjectMenuAction.Open; return true; }
            switch (objectType)
            {
                case ObjectTypeId.MissionTerminal: action = ObjectMenuAction.MissionList; return true;
                case ObjectTypeId.Mission: action = ObjectMenuAction.MissionDetails; return true;
                // VehicleControlDevice.idl and PetControlDeviceImplementation.cpp handle selection only.
                case ObjectTypeId.VehicleControlDevice:
                    action = sceneObject is IntangibleSceneObject vehicle && vehicle.IsActive ? ObjectMenuAction.VehicleStore : ObjectMenuAction.VehicleGenerate;
                    return true;
                case ObjectTypeId.PetControlDevice:
                case ObjectTypeId.DroidControlDevice:
                    action = sceneObject is IntangibleSceneObject pet && pet.IsActive ? ObjectMenuAction.PetStore : ObjectMenuAction.PetCall;
                    return true;
                case ObjectTypeId.CraftingTool: // Native requestCraftingSession command.
                case ObjectTypeId.CraftingStation: // Server chooses a compatible inventory tool.
                case ObjectTypeId.BankTerminal:
                    action = ObjectMenuAction.ClientUse;
                    return true;

                // These handlers implement Use but do not supply that entry themselves.
                case ObjectTypeId.Food:
                case ObjectTypeId.Drink:
                case ObjectTypeId.Pharmaceutical:
                case ObjectTypeId.PetMedicine:
                case ObjectTypeId.Firework:
                case ObjectTypeId.FireworkShow:
                case ObjectTypeId.TicketCollector:
                case ObjectTypeId.TravelTicket:
                case ObjectTypeId.Trap:
                case ObjectTypeId.CloningTerminal:
                case ObjectTypeId.InsuranceTerminal:
                case ObjectTypeId.NativeTravelTerminal:
                case ObjectTypeId.TravelTerminal:
                case ObjectTypeId.CampKit:
                case ObjectTypeId.ThrownWeapon:
                case ObjectTypeId.HeavyWeapon:
                case ObjectTypeId.SpecialHeavyWeapon:
                case ObjectTypeId.ResourceDeed:
                case ObjectTypeId.VeteranHarvesterDeed:
                case ObjectTypeId.Sign:
                case ObjectTypeId.SurveyTool:
                case ObjectTypeId.RepairTool:
                case ObjectTypeId.BuildingDeed:
                case ObjectTypeId.InstallationDeed:
                    action = ObjectMenuAction.Use;
                    return true;
                default:
                    if (HasTemplateUseAction(sceneObject?.TemplatePath))
                    {
                        action = ObjectMenuAction.Use;
                        return true;
                    }
                    action = null;
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

    }
}
