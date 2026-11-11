using Swg.Client.Core.Assets.Formats.Peft;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Particles
{
    /// <summary>Emitter motion and flock steering which Unity's particle modules cannot express.</summary>
    public sealed class ParticleEmitterPlayback : MonoBehaviour
    {
        ParticleSystem system;
        PeftEmitter definition;
        ParticleSystem.Particle[] particles;
        float clock;
        float playbackRate;
        public void Initialize(ParticleSystem system, PeftEmitter definition, float playbackRate)
        {
            this.system = system;
            this.definition = definition;
            this.playbackRate = playbackRate;
            var main = system.main;
            main.simulationSpeed = playbackRate;
            particles = new ParticleSystem.Particle[main.maxParticles];
        }

        void Update()
        {
            clock += Time.deltaTime * playbackRate;
            float duration = Mathf.Max(.01f, (definition.LifetimeMinimum + definition.LifetimeMaximum) * .5f);
            float age = Mathf.Repeat(Mathf.Max(0, clock - definition.Delay) / duration, 1);
            transform.localPosition = new Vector3(definition.Waves[0].Value(age), definition.Waves[1].Value(age), definition.Waves[2].Value(age));
            transform.localRotation = Quaternion.Euler(definition.Waves[3].Value(age) * 360, definition.Waves[4].Value(age) * 360, definition.Waves[5].Value(age) * 360);
        }

        void LateUpdate()
        {
            if (particles == null)
                return;
            int count = system.GetParticles(particles);
            float dt = Time.deltaTime * playbackRate;
            var center = definition.LocalSpace ? Vector3.zero : transform.position;
            for (int i = 0; i < count; i++)
            {
                float age = 1 - particles[i].remainingLifetime / particles[i].startLifetime;
                float random = (particles[i].randomSeed & 65535) / 65535f;
                float initialRotation = definition.RandomRotation ? random : 0;
                float sign = definition.RandomRotationDirection && random < .5f ? -1 : 1;
                if (string.IsNullOrEmpty(definition.MeshPath))
                    particles[i].rotation = sign * (initialRotation + definition.ParticleWaves[0].Value(age, random)) * 360;
                else
                    particles[i].rotation3D = new Vector3(definition.ParticleWaves[1].Value(age, random), definition.ParticleWaves[2].Value(age, random), definition.ParticleWaves[3].Value(age, random)) * 360;
                var keys = definition.Colors;
                int next = 0;
                while (next + 1 < keys.Length && keys[next].time < age)
                    next++;
                var left = keys[Mathf.Max(0, next - 1)];
                var right = keys[next];
                var color = Color.Lerp(left.color, right.color, right.time == left.time ? 0 : (age - left.time) / (right.time - left.time));
                color.a = Mathf.Clamp01(definition.Alpha.Value(age, random));
                particles[i].startColor = color;
                if (definition.FlockingType == 0)
                    continue;
                var position = particles[i].position;
                var separation = Vector3.zero;
                var alignment = Vector3.zero;
                var cohesion = Vector3.zero;
                int neighbors = 0;
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                        continue;
                    var delta = position - particles[j].position;
                    float square = delta.sqrMagnitude;
                    if (square > definition.SeparationDistance * definition.SeparationDistance * 4 || square < .0001f)
                        continue;
                    separation += delta / square;
                    alignment += particles[j].velocity;
                    cohesion += particles[j].position;
                    neighbors++;
                }

                var steering = separation * definition.SeparationGain;
                if (neighbors > 0)
                    steering += (alignment / neighbors - particles[i].velocity) * definition.AlignmentGain + (cohesion / neighbors - position) * definition.CohesionGain;
                var offset = position - center;
                var cage = new Vector3(Mathf.Max(.1f, definition.CageWidth), Mathf.Max(.1f, definition.CageHeight), Mathf.Max(.1f, definition.CageWidth));
                for (int axis = 0; axis < 3; axis++)
                    if (Mathf.Abs(offset[axis]) > cage[axis] * .5f)
                        steering[axis] -= Mathf.Sign(offset[axis]) * 4;
                var velocity = particles[i].velocity + steering * dt;
                float speed = Mathf.Max(.01f, definition.Waves[10].Value(0));
                particles[i].velocity = velocity.sqrMagnitude < .0001f ? Vector3.forward * speed : velocity.normalized * speed;
            }

            system.SetParticles(particles, count);
        }
    }
}
