using System.Collections.Generic;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Immutable native radial action metadata, from Core3 RadialOptions.h.
    /// Execution stays in ClientObjectInteractions; server-provided entries retain their own callback and text.</summary>
    public sealed class ObjectMenuAction
    {
        static readonly Dictionary<byte, ObjectMenuAction> actions = new Dictionary<byte, ObjectMenuAction>();
        public static readonly ObjectMenuAction VehicleEnterExit = Define(205, "", 1);
        public static readonly ObjectMenuAction Examine = Define(7, "@ui_radial:examine", 1);
        public static readonly ObjectMenuAction ConverseStart = Define(25, "@ui_radial:converse_start", 1);
        public static readonly ObjectMenuAction ConverseStop = Define(28, "@ui_radial:converse_stop", 1);
        public static readonly ObjectMenuAction Use = Define(20, "@ui_radial:item_use", 3);
        public static readonly ObjectMenuAction Open = Define(16, "@ui_radial:item_open", 1);
        public static readonly ObjectMenuAction MissionList = Define(33, "@ui_radial:terminal_mission_list", 3);
        public static readonly ObjectMenuAction MissionDetails = Define(34, "@ui_radial:mission_details", 3);
        public static readonly ObjectMenuAction MissionAbort = Define(62, "@ui_radial:mission_abort", 1);
        public static readonly ObjectMenuAction Listen = Define(113, "", 3);
        public static readonly ObjectMenuAction Watch = Define(114, "", 3);
        public static readonly ObjectMenuAction StopListen = Define(115, "", 3);
        public static readonly ObjectMenuAction StopWatch = Define(116, "", 3);
        public static readonly ObjectMenuAction Loot = Define(35, "@ui_radial:loot", 3);
        public static readonly ObjectMenuAction LootAll = Define(36, "@ui_radial:loot_all", 3);
        public static readonly ObjectMenuAction PetCall = Define(44, "@pet/pet_menu:menu_call", 3);
        public static readonly ObjectMenuAction PetStore = Define(59, "@pet/pet_menu:menu_store", 3);
        public static readonly ObjectMenuAction VehicleGenerate = Define(60, "@pet/pet_menu:menu_generate", 3);
        public static readonly ObjectMenuAction VehicleStore = Define(61, "@pet/pet_menu:menu_store", 3);

        // Crafting and bank Use roots are handled by the client rather than the server radial callback.
        public static readonly ObjectMenuAction ClientUse = new ObjectMenuAction(Use.Id, Use.Caption, 1);

        // Ensure definitions are registered before a lookup, even when no named field was accessed first.
        static ObjectMenuAction() { }

        public byte Id { get; }
        public string Caption { get; }
        public byte Callback { get; }

        ObjectMenuAction(byte id, string caption, byte callback)
        {
            Id = id;
            Caption = caption;
            Callback = callback;
        }

        static ObjectMenuAction Define(byte id, string caption, byte callback)
        {
            var action = new ObjectMenuAction(id, caption, callback);
            actions.Add(id, action);
            return action;
        }

        public static bool TryGet(byte id, out ObjectMenuAction action) => actions.TryGetValue(id, out action);
        public RadialMenuEntry ToEntry(byte index, byte parentIndex = 0) => new RadialMenuEntry(index, parentIndex, Id, Callback, Caption);
    }
}
