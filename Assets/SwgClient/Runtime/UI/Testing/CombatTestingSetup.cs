using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Characters;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Commands;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.SceneObjects;

namespace Swg.Client.Runtime.UI.Testing
{
    /// <summary>Offline server simulation for the CombatTesting scene. Uses the live dispatcher, command queue, packet decoders and animation path.</summary>
    public sealed class CombatTestingSetup : MonoBehaviour
    {
        [Serializable] public sealed class AttackDefinition
        {
            public string name;
            public string animation;
            public bool generated;
            public float range;
            public float speed;
            public float damage;
        }
        [Serializable] sealed class AttackDefinitions { public AttackDefinition[] commands; }
        public TextAsset commandDefinitions;
        readonly Dictionary<string, AttackDefinition> attackDefinitions = new Dictionary<string, AttackDefinition>(StringComparer.OrdinalIgnoreCase);
        public TMP_Dropdown animations;
        public TMP_Dropdown weaponStance;
        public TMP_Text status;
        public Slider dummyDistance;
        public Transform stage;
        public float recoverySeconds = 2;
        public bool Ready { get; private set; }
        readonly Queue<CommandQueueEnqueuePacket> waiting = new Queue<CommandQueueEnqueuePacket>();
        readonly HashSet<uint> cancelled = new HashSet<uint>();
        readonly List<AppearanceResources> resources = new List<AppearanceResources>();
        ClientCommands commands;
        ClientCombat combat;
        CommandCatalog catalog;
        CurrentPlayer player;
        WorldState state;
        CreatureSceneObject dummy;
        GameObject dummyObject;
        bool autoAttack;
        uint serverCounter = 0x40000000;
        string[] animationNames;
        public void Register(ContainerBuilder builder)
        {
            builder.RegisterInstance(new Action<CommandQueueEnqueuePacket>(Enqueue));
            builder.RegisterInstance(new Action<CommandQueueDequeuePacket>(packet => cancelled.Add(packet.ActionCounter)));
            builder.RegisterType<CommandDispatcher>().As<ICommandDispatcher>().SingleInstance();
        }
        IEnumerator Start()
        {
            foreach (var entry in JsonUtility.FromJson<AttackDefinitions>(commandDefinitions.text).commands) attackDefinitions[entry.name] = entry;
            player = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            commands = GameServices.Resolve<ClientCommands>();
            combat = GameServices.Resolve<ClientCombat>();
            catalog = GameServices.Resolve<CommandCatalog>();
            yield return new WaitUntil(() => catalog.Loading.IsCompleted && combat.Loading.IsCompleted);
            if (catalog.Loading.IsFaulted || combat.Loading.IsFaulted) yield break;
            player.Commands.Apply(values => values.AddRange(catalog.Definitions.Where(c => c.Category == "combat").Select(c => c.Name)));
            player.Creature.Ham = new HamPool { Health = new HamElement(1000, 1000, 0), Action = new HamElement(1000, 1000, 0), Mind = new HamElement(1000, 1000, 0) };
            player.Creature.Position = Vector3.zero;
            player.Creature.Rotation = Quaternion.identity;
            dummy = new CreatureSceneObject { SwgId = 9900, Name = "Target dummy", TemplatePath = "object/creature/player/shared_human_male.iff", Position = Vector3.forward * dummyDistance.value,
                Ham = new HamPool { Health = new HamElement(1000, 1000, 0), Action = new HamElement(1000, 1000, 0), Mind = new HamElement(1000, 1000, 0) } };
            state.SceneObjects.Add(dummy);
            foreach (var creature in new[] { player.Creature, dummy })
            {
                var load = GameServices.Resolve<WorldContentLoader>().LoadAppearanceAsync(creature);
                yield return new WaitUntil(() => load.IsCompleted);
                if (load.IsFaulted) { Debug.LogException(load.Exception.GetBaseException(), this); yield break; }
                var resource = new AppearanceResources(load.Result, new SwgMaterialFactory(GameServices.Resolve<TreRuntimeAssets>()), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
                resources.Add(resource);
                var instance = resource.Create(stage);
                instance.transform.localPosition = creature.Position;
                instance.transform.rotation = creature == dummy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                instance.AddComponent<ManagedSceneObject>().Bind(creature.SwgId);
                instance.AddComponent<SceneObjectAnimation>().Initialize(creature, instance.GetComponent<CharacterAnimation>());
                if (creature == dummy) dummyObject = instance;
            }
            GameServices.Resolve<ClientTargeting>().Select(dummy.SwgId);
            animationNames = combat.Loading.Result.Actions.Values.OrderBy(name => name).ToArray();
            animations.ClearOptions();
            animations.AddOptions(new[] { "Automatic attack" }.Concat(animationNames).ToList());
            var slots = FindAnyObjectByType<ActionBar>().slots;
            string[] defaults = { "attack", "unarmedHit1", "unarmedHit2", "unarmedSpinAttack1", "unarmedKnockdown1", "intimidate1", "warcry1", "oneHandMeleeHit1", "twoHandMeleeHit1", "polearmHit1", "bodyShot1", "peace" };
            var attacks = defaults.Select(catalog.Find).Where(c => c != null).ToArray();
            for (int i = 0; i < slots.Length && i < attacks.Length; i++) slots[i].Bind(attacks[i]);
            Ready = true;
            StartCoroutine(Process());
            status.text = "Offline combat � select an attack in the browser or action bar. All native playback actions are available below.";
        }
        public void SetDistance(float value)
        {
            if (dummy == null) return;
            dummy.Position = Vector3.forward * value;
            dummyObject.transform.localPosition = dummy.Position;
            state.SceneObjects.NotifyChanged(dummy, SceneObjectChange.Transform);
        }
        public void SetWeapon(int index)
        {
            if (!Ready) return;
            string[] paths = { "", "object/weapon/melee/knife/shared_knife_dagger.iff", "object/weapon/melee/2h_sword/shared_2h_sword_scythe.iff", "object/weapon/melee/polearm/shared_polearm_staff_metal.iff", "object/weapon/ranged/pistol/shared_pistol_cdef.iff", "object/weapon/ranged/rifle/shared_rifle_cdef.iff" };
            if (!state.SceneObjects.TryGet(9901, out var weapon)) { weapon = new SceneObject { SwgId = 9901 }; state.SceneObjects.Add(weapon); }
            weapon.TemplatePath = paths[index];
            player.Creature.WeaponSwgId = index == 0 ? default : weapon.SwgId;
            state.SceneObjects.NotifyChanged(player.Creature, SceneObjectChange.State);
        }
        public void Attack() => GameServices.Resolve<ICommandDispatcher>().Execute("attack", "", out _);
        public void Clear() => GameServices.Resolve<ClientCombatQueue>().Clear();
        public void Peace() => GameServices.Resolve<ClientCombatQueue>().Peace();
        public void ResetDummy()
        {
            if (!Ready) return;
            Peace();
            dummy.Ham.Health = new HamElement(1000, 1000, 0);
            state.SceneObjects.NotifyChanged(dummy, SceneObjectChange.Vitals);
        }
        void SetCombat(bool active)
        {
            foreach (var creature in new[] { player.Creature, dummy })
            {
                creature.States = active ? creature.States | CreatureState.Combat : creature.States & ~CreatureState.Combat;
                state.SceneObjects.NotifyChanged(creature, SceneObjectChange.State);
            }
        }
        static ObjectControllerPacket Packet(uint type, Action<BinaryWriter> payload)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x1bu); writer.Write(type); writer.Write(9000ul); writer.Write(0u); payload(writer);
            return ObjectControllerPacket.ReadFrom(new ByteReader(bytes.ToArray()));
        }
        void Complete(uint counter, float timer, uint error = 0)
            => commands.Receive(CommandQueueRemovePacket.ReadFrom(Packet(CommandQueueRemovePacket.ControllerType, w => { w.Write(counter); w.Write(timer); w.Write(error); w.Write(0u); })));
        void Enqueue(CommandQueueEnqueuePacket packet)
        {
            if (catalog == null) return;
            if (catalog.FindByCrc(packet.CommandCrc)?.Name == "peace")
            {
                autoAttack = false;
                while (waiting.Count > 0) Complete(waiting.Dequeue().Sequence, 0);
                SetCombat(false);
                Complete(packet.Sequence, 0);
            }
            else waiting.Enqueue(packet);
        }
        IEnumerator Process()
        {
            while (true)
            {
                yield return null;
                if (waiting.Count == 0 && !autoAttack) continue;
                CommandQueueEnqueuePacket packet;
                if (waiting.Count > 0) packet = waiting.Dequeue();
                else
                {
                    uint counter = ++serverCounter;
                    uint crc = ObjectTemplateNames.TemplateCrc("attack");
                    commands.Receive(CommandQueueAddPacket.ReadFrom(Packet(CommandQueueAddPacket.ControllerType, w => { w.Write(counter); w.Write(crc); })));
                    packet = new CommandQueueEnqueuePacket(player.SwgId.Value, counter, crc, "", dummy.SwgId.Value);
                }
                if (cancelled.Remove(packet.Sequence)) continue;
                var command = catalog.FindByCrc(packet.CommandCrc);
                attackDefinitions.TryGetValue(command?.Name ?? "", out var definition);
                float maximumRange = definition != null && definition.range > 0 ? definition.range : weaponStance.value >= 4 ? 64 : 5;
                if ((dummy.Position - player.Creature.Position).magnitude > maximumRange)
                {
                    Complete(packet.Sequence, 0, 1);
                    autoAttack = false;
                    status.text = "Target is out of melee range. Combat has not started.";
                    continue;
                }
                autoAttack = true;
                SetCombat(true);
                string automatic = definition == null ? weaponStance.value >= 4 ? "fire_1_single_light" : "attack_mid_center_light_0"
                    : definition.animation + (definition.generated ? "_light" : "");
                string action = animations.value == 0 ? automatic : animationNames[animations.value - 1];
                float recovery = recoverySeconds * (definition?.speed ?? 1);
                combat.Receive(CombatActionPacket.ReadFrom(Packet(CombatActionPacket.ControllerType, w =>
                {
                    w.Write(ObjectTemplateNames.TemplateCrc(action)); w.Write(player.SwgId.Value); w.Write(player.Creature.WeaponSwgId.Value); w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);
                    w.Write((ushort)1); w.Write(dummy.SwgId.Value); w.Write((byte)0); w.Write((byte)1); w.Write((byte)0);
                })));
                dummy.Ham.Health = new HamElement(Math.Max(0, dummy.Ham.Health.Current - (int)(10 * (definition?.damage ?? 1))), 1000, 0);
                state.SceneObjects.NotifyChanged(dummy, SceneObjectChange.Vitals);
                Complete(packet.Sequence, recovery);
                status.text = action + " � server simulation, " + recovery + "s recovery";
                yield return new WaitForSecondsRealtime(recovery);
            }
        }
        void OnDestroy() { foreach (var resource in resources) resource.Dispose(); }
    }
}
