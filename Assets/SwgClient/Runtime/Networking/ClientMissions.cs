using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Mission;
using Swg.Client.Runtime.Localization;
namespace Swg.Client.Runtime.Networking
{
    /// <summary>Mission requests and views of the server-owned mission-bag/datapad containment graph.</summary>
    public sealed class ClientMissions : IDisposable
    {
        readonly WorldState state;
        readonly StringTableLocalizer strings;
        readonly Action<MissionListRequestPacket> request;
        readonly Action<MissionAcceptPacket> accept;
        readonly Action<MissionAbortPacket> abort;
        byte sequence;
        SwgId pendingAccept;
        public SwgId TerminalSwgId { get; private set; }
        public SwgId SelectedSwgId { get; private set; }
        public SwgId AbortCandidate { get; private set; }
        public bool BrowserOpen { get; private set; }
        public bool DetailsOpen { get; private set; }
        public bool Loading { get; private set; }
        public bool Accepting => !pendingAccept.IsNone;
        public event Action Changed;
        public MissionSceneObject Selected => state.SceneObjects.TryGet<MissionSceneObject>(SelectedSwgId, out var mission) ? mission : null;
        SwgId MissionBag => state.SceneObjects.ChildrenOf(state.PlayerSwgId).FirstOrDefault(item => item.TemplatePath?.StartsWith("object/tangible/mission_bag/", StringComparison.Ordinal) == true)?.SwgId ?? default;
        IEnumerable<MissionSceneObject> Offers => state.SceneObjects.ChildrenOf(MissionBag).OfType<MissionSceneObject>();
        public IEnumerable<MissionSceneObject> Available => Offers.Where(mission => mission.RefreshCounter == sequence && mission.MissionTypeCrc != 0 && !string.IsNullOrEmpty(mission.Title));
        public ClientMissions(WorldState state, StringTableLocalizer strings, Action<MissionListRequestPacket> request, Action<MissionAcceptPacket> accept, Action<MissionAbortPacket> abort)
        {
            this.state = state; this.strings = strings; this.request = request; this.accept = accept; this.abort = abort;
            state.SceneObjects.ObjectChanged += ObjectChanged;
            state.SceneObjects.Removed += Removed;
            state.SceneStarted += Close;
        }
        public void Open(SwgId terminal)
        {
            TerminalSwgId = terminal;
            BrowserOpen = true;
            DetailsOpen = false;
            Refresh();
        }
        public void Refresh()
        {
            if (Loading || TerminalSwgId.IsNone) return;
            sequence = unchecked((byte)(sequence + 1));
            SelectedSwgId = default;
            Loading = true;
            Changed?.Invoke();
            request(new MissionListRequestPacket(state.PlayerSwgId, TerminalSwgId, sequence));
        }
        void ObjectChanged(SceneObject item, SceneObjectChange changes)
        {
            if (item is not MissionSceneObject) return;
            if (Loading)
            {
                var offers = Offers.ToArray();
                if (offers.Length > 0 && offers.All(mission => mission.RefreshCounter == sequence)) Loading = false;
            }
            if (item.SwgId == pendingAccept && IsAccepted((MissionSceneObject)item)) { pendingAccept = default; DetailsOpen = false; }
            Changed?.Invoke();
        }
        void Removed(SceneObject item)
        {
            if (item is not MissionSceneObject) return;
            if (SelectedSwgId == item.SwgId) { SelectedSwgId = default; DetailsOpen = false; }
            if (AbortCandidate == item.SwgId) AbortCandidate = default;
            Changed?.Invoke();
        }
        public void Select(SwgId id) { SelectedSwgId = id; Changed?.Invoke(); }
        public void ShowDetails(SwgId id) { if (!state.SceneObjects.TryGet<MissionSceneObject>(id, out _)) return; SelectedSwgId = id; DetailsOpen = true; Changed?.Invoke(); }
        public bool IsAccepted(MissionSceneObject mission) => state.AcceptedMissions.Any(item => item.SwgId == mission.SwgId);
        public bool CanAccept => !Accepting && Selected != null && Selected.ParentSwgId == MissionBag && !TerminalSwgId.IsNone;
        public void Accept()
        {
            if (!CanAccept) return;
            pendingAccept = SelectedSwgId;
            accept(new MissionAcceptPacket(state.PlayerSwgId, SelectedSwgId, TerminalSwgId));
            Changed?.Invoke();
        }
        public void Receive(MissionAcceptResponsePacket packet)
        {
            if (packet.MissionSwgId != pendingAccept.Value) return;
            // Core3 sends this response even when acceptance was refused. Containment determines success.
            pendingAccept = default;
            if (Selected != null && IsAccepted(Selected)) DetailsOpen = false;
            Changed?.Invoke();
        }
        public void Receive(MissionAbortResponsePacket packet) => Changed?.Invoke(); // Removal is authoritative, not this unconditional acknowledgement.
        public void Receive(PopulateMissionBrowserPacket packet) => Changed?.Invoke(); // Core3 normally completes through per-mission refresh counters.
        public void RequestAbort(SwgId id)
        {
            if (!state.SceneObjects.TryGet<MissionSceneObject>(id, out var mission) || !IsAccepted(mission)) return;
            AbortCandidate = id; Changed?.Invoke();
        }
        public void ConfirmAbort()
        {
            if (AbortCandidate.IsNone) return;
            var id = AbortCandidate; AbortCandidate = default;
            abort(new MissionAbortPacket(state.PlayerSwgId, id)); Changed?.Invoke();
        }
        public void CancelAbort() { AbortCandidate = default; Changed?.Invoke(); }
        public void CloseDetails() { DetailsOpen = false; Changed?.Invoke(); }
        public void Close() { BrowserOpen = false; DetailsOpen = false; Loading = false; AbortCandidate = default; pendingAccept = default; Changed?.Invoke(); }
        public string Title(MissionSceneObject mission) => strings.Localize(mission.Title);
        public string Description(MissionSceneObject mission) => strings.Localize(mission.Description);
        public string Category(MissionSceneObject mission) => mission.MissionTypeCrc switch
        {
            0x74ef9be3 => "Destroy", 0xe5c27ec6 => "Deliver", 0x2904f372 => "Bounty", 0xe5f6dc59 => "Crafting",
            0x906999a2 => "Hunting", 0x4ad93196 => "Musician", 0x0f067b37 => "Dancer", 0x34f4c2e4 => "Recon", 0x19c9fac1 => "Survey", _ => "Other"
        };
        public string Location(MissionSceneObject mission)
        {
            Vector3 player = state.SceneObjects.TryGet<SpatialSceneObject>(state.PlayerSwgId, out _) ? state.SceneObjects.GetServerWorldPose(state.PlayerSwgId).position : Vector3.zero;
            var delta = mission.StartPosition - player;
            var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int direction = Mathf.RoundToInt(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg / 45 + 8) % 8;
            return Mathf.RoundToInt(new Vector2(delta.x, delta.z).magnitude) + "m to the " + directions[direction];
        }
        public string TargetPath(MissionSceneObject mission) => state.Templates?.Resolve(mission.TargetTemplateCrc);
        public string DetailText(MissionSceneObject mission) => "Target:\n<align=right>" + strings.Localize(mission.TargetName) + "</align>\nLocation:\n<align=right>" + Location(mission) + "</align>\nCreator:\n<align=right>" + mission.Creator + "</align>\nReward:\n<align=right>" + mission.Reward + " Credits</align>\nDifficulty:\n<align=right>" + mission.Difficulty + "</align>";
        public void Dispose() { state.SceneObjects.ObjectChanged -= ObjectChanged; state.SceneObjects.Removed -= Removed; state.SceneStarted -= Close; }
    }
}

