using System.Linq;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Scales only the vehicle's thruster effects with rendered travel speed. Previews remain at idle.</summary>
    public sealed class VehicleExhaust : MonoBehaviour
    {
        Transform[] exhausts;
        Vector3[] scales;
        Vector3 previousPosition;
        CreatureSceneObject vehicle;
        float templateSpeed;
        float throttle;
        public float Throttle => throttle;

        public void Initialize(Transform[] exhausts)
        {
            this.exhausts = exhausts;
            scales = exhausts.Select(effect => effect.localScale).ToArray();
            foreach (var effect in exhausts)
                foreach (var system in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = system.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                }
            ApplyThrottle(0);
            enabled = false;
        }

        public void Bind(CreatureSceneObject vehicle, float templateSpeed)
        {
            this.vehicle = vehicle;
            this.templateSpeed = templateSpeed;
            previousPosition = transform.position;
            enabled = true;
        }

        void LateUpdate()
        {
            float seconds = Time.deltaTime;
            var delta = transform.position - previousPosition;
            previousPosition = transform.position;
            if (seconds <= 0) return;
            // Owner-only movement baselines arrive on the rider. Other vehicles use native template speed.
            var rider = vehicle.Children.OfType<CreatureSceneObject>().FirstOrDefault();
            float maximum = rider?.MovementSpeed ?? (vehicle.MovementSpeed > 0 ? vehicle.MovementSpeed : templateSpeed);
            float speed = new Vector2(delta.x, delta.z).magnitude / seconds;
            float target = maximum > 0 ? Mathf.Clamp01(speed / maximum) : 0;
            ApplyThrottle(Mathf.MoveTowards(throttle, target, seconds * 4));
        }

        internal void ApplyThrottle(float value)
        {
            throttle = Mathf.Clamp01(value);
            // Retain a short idle flame instead of switching the effect off.
            float scale = Mathf.Lerp(.15f, 1, throttle);
            for (int i = 0; i < exhausts.Length; i++)
                exhausts[i].localScale = scales[i] * scale;
        }
    }
}
