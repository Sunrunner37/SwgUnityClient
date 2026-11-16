using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Packets.Chat;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking.Chat;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Tracks server-confirmed listening/watching, without awarding or predicting actual buffs.</summary>
    public sealed class ClientEntertainment : IDisposable
    {
        public sealed class Session
        {
            public readonly CreatureSceneObject Performer;
            public readonly EntertainerBuffEstimate Estimate;
            public bool Grouped;
            public int StrengthPerPulse;
            public Session(CreatureSceneObject performer, double now)
            { Performer = performer; Estimate = new EntertainerBuffEstimate(now); }
        }
        readonly CurrentPlayer player;
        readonly SceneObjectRegistry objects;
        readonly ClientChat chat;
        readonly ClientCombat combat;
        int[] strength;
        public Task Loading { get; }
        public Session Music { get; private set; }
        public Session Dance { get; private set; }
        public ClientEntertainment(CurrentPlayer player, SceneObjectRegistry objects, ClientChat chat, ClientCombat combat, TreRuntimeAssets assets)
        {
            this.player = player;
            this.objects = objects;
            this.chat = chat;
            this.combat = combat;
            Loading = Load(assets);
            chat.SystemMessageReceived += Receive;
            combat.FlourishReceived += Flourish;
        }
        async Task Load(TreRuntimeAssets assets) => strength = await assets.ReadAsync(loader =>
            DataTableFile.Parse(loader.Read("datatables/performance/performance.iff")).Rows.Select(row => row.Get<int>("healShockWound")).ToArray());
        public void Refresh(double now)
        {
            var listener = player.Creature;
            if (listener == null) { Music = Dance = null; return; }
            if (listener.ListenSwgId.IsNone) Music = null;
            else if (Music?.Performer.SwgId != listener.ListenSwgId)
                Music = Start(listener.ListenSwgId, false, now);
            if (Music != null && !Valid(Music, false)) Music = null;
            if (Dance != null && !Valid(Dance, true)) Dance = null;
            Advance(Music, now);
            Advance(Dance, now);
        }
        bool Valid(Session session, bool dance) => objects.TryGet<CreatureSceneObject>(session.Performer.SwgId, out var performer)
            && performer == session.Performer && (dance ? performer.IsDancing : performer.IsPlayingMusic)
            && Vector3.Distance(objects.GetServerWorldPose(player.SwgId).position, objects.GetServerWorldPose(performer.SwgId).position) <= 60;
        Session Start(SwgId id, bool dance, double now)
        {
            if (!objects.TryGet<CreatureSceneObject>(id, out var performer) || !(dance ? performer.IsDancing : performer.IsPlayingMusic)) return null;
            // Other players' skills are owner-only. Do not claim that a performer is a master.
            // If skills ARE available, suppress the master estimate for a known non-master.
            if (performer.Skills.Count > 0 && !performer.Skills.Contains(dance ? "social_dancer_master" : "social_musician_master")) return null;
            return new Session(performer, now);
        }
        void Advance(Session session, double now)
        {
            if (session == null || strength == null) return;
            session.Grouped = !player.Creature.GroupSwgId.IsNone && player.Creature.GroupSwgId == session.Performer.GroupSwgId;
            int index = session.Performer.PerformanceType - 1;
            session.StrengthPerPulse = index >= 0 && index < strength.Length ? strength[index] : 0;
            session.Estimate.Advance(now, session.StrengthPerPulse, session.Grouped);
        }
        void Flourish(SwgId id)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            foreach (var session in new[] { Music, Dance })
            {
                if (session == null || session.Performer.SwgId != id) continue;
                Advance(session, now);
                session.Estimate.Flourish(now, session.StrengthPerPulse, session.Grouped);
            }
        }
        void Receive(ChatSystemPacket packet)
        {
            foreach (var parameter in packet.Parameters.Parameters ?? Array.Empty<ChatParameterData>())
            {
                if (parameter.Type != 1 || parameter.StringId.StringId.File != "performance") continue;
                string key = parameter.StringId.StringId.Key;
                var target = new SwgId(parameter.StringId.Target.SwgId);
                if (key == "music_listen_self") Music = Start(target, false, Time.realtimeSinceStartupAsDouble);
                else if (key == "dance_watch_self") Dance = Start(target, true, Time.realtimeSinceStartupAsDouble);
                else if (key == "dance_watch_stop_self" || key == "dance_stop_other") Dance = null;
            }
            if (packet.Message == "@performance:dance_watch_stop_self") Dance = null;
        }
        public void Dispose()
        {
            chat.SystemMessageReceived -= Receive;
            combat.FlourishReceived -= Flourish;
            Music = Dance = null;
        }
    }
}

