using Swg.Client.Runtime.Assets.Appearances;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Peft;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Particles
{
    /// <summary>Instances child appearances at authored particle ages; IDs are particle seeds, not scene-object IDs.</summary>
    public sealed class ParticleAttachments : MonoBehaviour
    {
        sealed class Instance
        {
            public bool[] Spawned;
            public GameObject[] Objects;
            public Vector3 Position;
        }

        ParticleSystem system;
        ParticleSystem.Particle[] particles;
        PeftAttachment[] definitions;
        AppearanceResources[] resources;
        readonly Dictionary<uint, Instance> instances = new Dictionary<uint, Instance>();
        readonly HashSet<uint> alive = new HashSet<uint>();
        readonly List<uint> dead = new List<uint>();
        public void Initialize(ParticleSystem system, PeftAttachment[] definitions, AppearanceResources[] resources)
        {
            this.system = system;
            this.definitions = definitions;
            this.resources = resources;
            particles = new ParticleSystem.Particle[system.main.maxParticles];
        }

        void LateUpdate()
        {
            int count = system.GetParticles(particles);
            alive.Clear();
            for (int p = 0; p < count; p++)
            {
                var particle = particles[p];
                uint id = particle.randomSeed;
                alive.Add(id);
                if (!instances.TryGetValue(id, out var instance))
                    instances.Add(id, instance = new Instance { Spawned = new bool[definitions.Length], Objects = new GameObject[definitions.Length] });
                instance.Position = system.main.simulationSpace == ParticleSystemSimulationSpace.Local ? transform.TransformPoint(particle.position) : particle.position;
                float age = 1 - particle.remainingLifetime / particle.startLifetime;
                for (int i = 0; i < definitions.Length; i++)
                {
                    float start = Mathf.Lerp(definitions[i].StartMinimum, definitions[i].StartMaximum, (id & 65535) / 65535f);
                    if (!instance.Spawned[i] && (definitions[i].Spawn == 0 || definitions[i].Spawn == 2 && age >= start))
                        Spawn(instance, i);
                    if (instance.Objects[i] != null)
                        instance.Objects[i].transform.position = instance.Position;
                }
            }

            dead.Clear();
            foreach (var pair in instances)
                if (!alive.Contains(pair.Key))
                    dead.Add(pair.Key);
            foreach (var id in dead)
            {
                var instance = instances[id];
                for (int i = 0; i < definitions.Length; i++)
                {
                    if (definitions[i].Spawn == 1)
                        Spawn(instance, i);
                    if (instance.Objects[i] == null)
                        continue;
                    if (definitions[i].KillWhenParticleDies)
                        Destroy(instance.Objects[i]);
                    else
                        StartCoroutine(Finish(instance.Objects[i]));
                }

                instances.Remove(id);
            }
        }

        void Spawn(Instance instance, int index)
        {
            instance.Spawned[index] = true;
            var child = resources[index].Create(transform);
            child.transform.position = instance.Position;
            instance.Objects[index] = child;
        }

        static IEnumerator Finish(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>();
            foreach (var system in systems)
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            while (root != null && systems.Any(system => system != null && system.IsAlive(false)))
                yield return null;
            if (root != null)
                Destroy(root);
        }
    }
}
