using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Routes server-selected attacks and reactions to each rendered creature on the main thread. Combat state and damage remain server-owned.</summary>
    public sealed class ClientCombat : IDisposable
    {
        readonly WorldState state;
        readonly SynchronizationContext context;
        readonly Dictionary<SwgId, Action<string, string, bool>> listeners = new Dictionary<SwgId, Action<string, string, bool>>();
        readonly Task<CbtmFile> loading;
        bool disposed;
        int variation;
        public Task<CbtmFile> Loading => loading;
        public event Action<CombatActionPacket> ActionReceived;
        public event Action<CombatActionPacket, CbtmFile.Playback> PlaybackRequested;
        public ClientCombat(TreRuntimeAssets assets, WorldState state)
        {
            this.state = state;
            context = SynchronizationContext.Current;
            loading = assets.ReadAsync(loader => CbtmFile.Parse(loader.Read("combat/combat_manager.iff"), loader.Read));
        }
        public void Subscribe(SwgId id, Action<string, string, bool> callback)
        {
            listeners.TryGetValue(id, out var current);
            listeners[id] = current + callback;
        }
        public void Unsubscribe(SwgId id, Action<string, string, bool> callback)
        {
            if (!listeners.TryGetValue(id, out var current)) return;
            current -= callback;
            if (current == null) listeners.Remove(id); else listeners[id] = current;
        }
        public string WeaponStance(CreatureSceneObject creature, SwgId weapon = default)
        {
            if (weapon.IsNone) weapon = creature.WeaponSwgId;
            if (!state.SceneObjects.TryGet(weapon, out var item)) return "";
            string path = item.TemplatePath ?? "";
            if (path.Contains("/pistol/")) return "pistol";
            if (path.Contains("/rifle/") || path.Contains("/carbine/") || path.Contains("/heavy/")) return "rifle";
            if (path.Contains("/polearm/") || path.Contains("/lance/")) return "polearm";
            if (path.Contains("/2h_sword/") || path.Contains("/axe/")) return "sword_2h";
            if (path.Contains("/sword/") || path.Contains("/knife/") || path.Contains("/baton/")) return "sword_1h";
            return "";
        }
        public void Play(SwgId id, string action, bool reaction = false, SwgId weapon = default)
        {
            if (string.IsNullOrEmpty(action) || !listeners.TryGetValue(id, out var callback)) return;
            string stance = state.SceneObjects.TryGet<CreatureSceneObject>(id, out var creature) ? WeaponStance(creature, weapon) : "";
            callback(action, stance, reaction);
        }
        public void Receive(CombatActionPacket packet) => context.Post(_ => Apply(packet), null);
        async void Apply(CombatActionPacket packet)
        {
            try
            {
                var table = await loading;
                if (disposed) return;
                bool attackerPlayed = false;
                foreach (var defender in packet.Defenders)
                {
                    if (!table.TryGet(packet.AnimationCrc, defender.Hit, packet.AttackerPosture, defender.Posture, variation++, out var playback)) continue;
                    if (!attackerPlayed)
                    {
                        PlaybackRequested?.Invoke(packet, playback);
                        Play(packet.AttackerSwgId, playback.Attacker, weapon: packet.WeaponSwgId);
                        attackerPlayed = true;
                    }
                    Play(defender.SwgId, playback.Defender, reaction: true);
                }
                if (!attackerPlayed && table.TryGet(packet.AnimationCrc, 0, packet.AttackerPosture, 0, variation++, out var solo))
                {
                    PlaybackRequested?.Invoke(packet, solo);
                    Play(packet.AttackerSwgId, solo.Attacker, weapon: packet.WeaponSwgId);
                }
                ActionReceived?.Invoke(packet);
            }
            catch (Exception error) { Debug.LogException(error); }
        }
        public void Receive(FlourishPacket packet) => context.Post(_ =>
        {
            if (!disposed && packet.FlourishId >= 1 && packet.FlourishId <= 8)
                Play(packet.ReceiverSwgId, "skill_action_" + packet.FlourishId);
        }, null);
        public void Receive(AnimationPacket packet) => context.Post(_ => { if (!disposed) Play(packet.ReceiverSwgId, packet.Animation); }, null);
        public void Dispose() { disposed = true; listeners.Clear(); ActionReceived = null; PlaybackRequested = null; }
    }
}
