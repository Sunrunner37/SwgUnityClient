using System.Collections;
using System.Linq;
using UnityEngine;
namespace Swg.Client.Runtime.World.SceneObjects
{
    public sealed class VehicleSummon : MonoBehaviour
    {
        IEnumerator Start()
        {
            var visuals = GetComponentsInChildren<Renderer>().Where(renderer => renderer.GetComponent<ParticleSystem>() == null).Select(renderer => renderer.transform).Distinct().ToArray();
            var positions = visuals.Select(item => item.localPosition).ToArray();
            const float duration = 1.2f;
            for (float age = 0; age < duration; age += Time.deltaTime)
            {
                float depth = (1 - Mathf.SmoothStep(0, 1, age / duration)) * 1.8f;
                for (int i = 0; i < visuals.Length; i++) if (visuals[i] != null) visuals[i].localPosition = positions[i] - Vector3.up * depth;
                yield return null;
            }
            for (int i = 0; i < visuals.Length; i++) if (visuals[i] != null) visuals[i].localPosition = positions[i];
            Destroy(this);
        }
    }
}
