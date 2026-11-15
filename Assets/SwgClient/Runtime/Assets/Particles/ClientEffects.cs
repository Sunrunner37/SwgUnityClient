using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Cef;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Swh;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.Assets.Particles
{
    /// <summary>Game-owned cache and temporary playback for native client effects, playback particles and motion trails.</summary>
    public sealed class ClientEffects : MonoBehaviour, IDisposable
    {
        sealed class Effect
        {
            public AppearanceData[] Appearances;
            public float[] Durations;
            public AppearanceResources[] Resources;
        }
        sealed class Swoosh
        {
            public SwhFile Definition;
            public TextureData Texture;
            public bool Additive;
            public Material Material;
        }
        readonly Dictionary<string, Task<Effect>> effects = new Dictionary<string, Task<Effect>>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Task<Swoosh>> swooshes = new Dictionary<string, Task<Swoosh>>(StringComparer.OrdinalIgnoreCase);
        TreRuntimeAssets assets;
        WorldState state;
        ClientCombat combat;
        SynchronizationContext context;
        bool disposed;
        public void Initialize(TreRuntimeAssets assets, WorldState state, ClientCombat combat)
        {
            this.assets = assets;
            this.state = state;
            this.combat = combat;
            context = SynchronizationContext.Current;
            combat.PlaybackRequested += PlayCombat;
        }
        public void Receive(PlayClientEffectObjectPacket packet) => context.Post(_ =>
        {
            if (!disposed) PlayObject(packet.File, packet.SwgId, packet.Auxiliary);
        }, null);
        public void Receive(PlayClientEffectLocPacket packet) => context.Post(_ =>
        {
            if (disposed || ObjectTemplateNames.TemplateCrc(packet.Planet) != state.PlanetCrc) return;
            var position = packet.Position - state.RenderOrigin;
            if (ManagedSceneObject.TryGetObjectFrom(packet.CellSwgId, out var cell)) position = cell.transform.TransformPoint(packet.Position);
            StartCoroutine(PlayAt(packet.EffectFile, null, position));
        }, null);
        public void PlayObject(string file, SwgId swgId, string hardpoint = "")
        {
            if (disposed || !ManagedSceneObject.TryGetObjectFrom(swgId, out var root)) return;
            StartCoroutine(PlayAt(file, Attachment(root.transform, hardpoint), Vector3.zero));
        }
        static Transform Attachment(Transform root, string name)
        {
            if (string.IsNullOrEmpty(name)) return root;
            return root.GetComponentsInChildren<Transform>().FirstOrDefault(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase))
                ?? root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "hold_r") ?? root;
        }
        Task<Effect> Load(string file)
        {
            // Playback duration is per instance. Only CEF durations belong to the cached definition.
            if (!effects.TryGetValue(file, out var loading))
            {
                loading = assets.ReadAsync(loader =>
                {
                    var entries = file.EndsWith(".cef", StringComparison.OrdinalIgnoreCase)
                        ? CefFile.Parse(loader.Read(file)).Appearances.ToArray()
                        : new[] { new CefFile.Appearance(file, 0) };
                    return new Effect { Appearances = entries.Select(e => AppearanceLoader.LoadDetailed(loader, e.Path)).ToArray(), Durations = entries.Select(e => e.Duration).ToArray() };
                });
                effects.Add(file, loading);
            }
            return loading;
        }
        IEnumerator PlayAt(string file, Transform attachment, Vector3 position, float duration = 0, Transform target = null, bool directed = false)
        {
            var loading = Load(file);
            bool attached = attachment != null;
            yield return new WaitUntil(() => loading.IsCompleted);
            if (disposed || attached && attachment == null) yield break;
            if (loading.IsFaulted) { Debug.LogException(loading.Exception.GetBaseException(), this); yield break; }
            var effect = loading.Result;
            if (effect.Resources == null)
                effect.Resources = effect.Appearances.Select(a => new AppearanceResources(a, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"))).ToArray();
            for (int i = 0; i < effect.Resources.Length; i++)
            {
                var root = effect.Resources[i].Create(transform, false);
                root.transform.position = attached ? attachment.position : position;
                root.transform.rotation = attached ? attachment.rotation : Quaternion.identity;
                StartCoroutine(Lifetime(root, attachment, duration > 0 ? duration : effect.Durations[i], target, directed));
            }
        }
        IEnumerator Lifetime(GameObject root, Transform attachment, float duration, Transform target, bool directed)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>();
            float end = Time.time + Mathf.Max(.01f, duration);
            while (root != null && Time.time < end)
            {
                if (attachment != null)
                {
                    root.transform.position = attachment.position;
                    root.transform.rotation = attachment.rotation;
                }
                // Native directional emitters emit along +Y. Aim that axis down the weapon/target line.
                if (directed)
                {
                    Vector3 direction = target != null ? target.position + Vector3.up - root.transform.position : attachment != null ? attachment.forward : Vector3.forward;
                    if (direction.sqrMagnitude > .001f) root.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                }
                yield return null;
            }
            foreach (var system in systems) if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            while (root != null && systems.Any(s => s != null && s.IsAlive(false))) yield return null;
            if (root != null) Destroy(root);
        }
        void PlayCombat(CombatActionPacket packet, CbtmFile.Playback playback)
        {
            if (!ManagedSceneObject.TryGetObjectFrom(packet.AttackerSwgId, out var attacker)) return;
            Transform target = null;
            if (packet.Defenders.Length > 0 && ManagedSceneObject.TryGetObjectFrom(packet.Defenders[0].SwgId, out var defender)) target = defender.transform;
            foreach (var particle in playback.Particles)
                StartCoroutine(PlayAt(particle.Path, Attachment(attacker.transform, particle.Hardpoint), Vector3.zero, particle.Duration, target, true));
            if (playback.HasTrails && packet.Trails != 0 && !string.IsNullOrEmpty(playback.Attacker))
                StartCoroutine(AwaitTrails(attacker, packet.Trails));
        }
        IEnumerator AwaitTrails(GameObject attacker, byte flags)
        {
            var animation = attacker.GetComponent<CharacterAnimation>();
            if (animation == null) yield break;
            int actionId = animation.ActionId + 1;
            SkeletalAnimation clip = null;
            void Started(SkeletalAnimation value) { if (animation.ActionId == actionId) clip = value; }
            animation.ActionStarted += Started;
            try
            {
                float timeout = Time.time + 3;
                while (clip == null && attacker != null && animation.ActionId <= actionId && Time.time < timeout) yield return null;
            }
            finally { if (animation != null) animation.ActionStarted -= Started; }
            if (clip == null || attacker == null || animation.ActionId != actionId) yield break;
            // Default trails follow authored swing messages rather than enabling every limb.
            foreach (var swing in clip.Messages.Where(m => m.Name.StartsWith("uswing_", StringComparison.Ordinal)))
            {
                string hardpoint = swing.Name.Substring("uswing_".Length);
                int bit = TrailMask(hardpoint);
                if (flags != 255 && (flags & bit) == 0) continue;
                float hit = clip.Messages.Where(m => m.Name == "uhit_" + hardpoint && m.Time >= swing.Time).Select(m => m.Time).DefaultIfEmpty(clip.Duration).First();
                StartCoroutine(Trail(attacker.transform, animation, actionId, hardpoint, swing.Time, Mathf.Min(clip.Duration, hit + .12f)));
            }
        }
        // Core3 CombatManager's trail bitmask; native kick messages use lankle/rankle.
        internal static int TrailMask(string hardpoint) => hardpoint.Contains("hold_l") ? 4 : hardpoint.Contains("hold_r") ? 8
            : hardpoint.StartsWith("l", StringComparison.OrdinalIgnoreCase) || hardpoint.EndsWith("_l", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        IEnumerator Trail(Transform actor, CharacterAnimation animation, int actionId, string hardpoint, float start, float stop)
        {
            const string path = "appearance/sw_basic_bloody_red.swh";
            if (!swooshes.TryGetValue(path, out var loading))
            {
                loading = assets.ReadAsync(loader =>
                {
                    var definition = SwhFile.Parse(loader.Read(path));
                    TextureData texture = null;
                    if (ShtFile.TryGetMainTexture(loader.Read(definition.Shader), out var diffuse)) texture = TextureData.Load(loader, diffuse);
                    return new Swoosh { Definition = definition, Texture = texture, Additive = ShtFile.UsesAdditiveBlending(loader, definition.Shader) };
                });
                swooshes.Add(path, loading);
            }
            yield return new WaitUntil(() => loading.IsCompleted);
            if (loading.IsFaulted) { Debug.LogException(loading.Exception.GetBaseException(), this); yield break; }
            while (actor != null && animation.ActionId == actionId && animation.ActionTime < start && animation.IsPlayingAction) yield return null;
            if (actor == null || animation.ActionId != actionId || !animation.IsPlayingAction || animation.ActionTime >= stop) yield break;
            var source = loading.Result;
            if (source.Material == null)
            {
                source.Material = new Material(Shader.Find("SWG/Particle"));
                source.Material.SetTexture("_BaseMap", source.Texture == null ? Texture2D.whiteTexture : assets.Texture(source.Texture, TextureWrapMode.Clamp));
                source.Material.SetFloat("_DstBlend", source.Additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            }
            var anchor = Attachment(actor, hardpoint);
            var root = new GameObject("Combat trail " + hardpoint);
            root.transform.SetParent(transform, false);
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = source.Material;
            trail.time = .18f / Mathf.Max(.01f, animation.ActionPlaybackRate);
            trail.widthMultiplier = source.Definition.Width;
            trail.minVertexDistance = .015f;
            trail.startColor = source.Definition.Color;
            trail.endColor = new Color(source.Definition.Color.r, source.Definition.Color.g, source.Definition.Color.b, 0);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            root.transform.position = anchor.position;
            trail.Clear();
            trail.emitting = true;
            while (anchor != null && animation != null && animation.ActionId == actionId && animation.IsPlayingAction && animation.ActionTime < stop)
            {
                root.transform.position = anchor.position;
                trail.time = .18f / Mathf.Max(.01f, animation.ActionPlaybackRate);
                yield return null;
            }
            trail.emitting = false;
            Destroy(root, trail.time);
        }
        void Release()
        {
            if (disposed) return;
            disposed = true;
            combat.PlaybackRequested -= PlayCombat;
            StopAllCoroutines();
            foreach (var task in effects.Values.Where(t => t.Status == TaskStatus.RanToCompletion))
                if (task.Result.Resources != null) foreach (var resource in task.Result.Resources) resource.Dispose();
            foreach (var task in swooshes.Values.Where(t => t.Status == TaskStatus.RanToCompletion))
                if (task.Result.Material != null) Destroy(task.Result.Material);
        }
        public void Dispose()
        {
            Release();
            if (this != null) { if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject); }
        }
        void OnDestroy() => Release();
    }
}
