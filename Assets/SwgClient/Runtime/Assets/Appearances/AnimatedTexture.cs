using System.Collections;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Plays native shader texture frames without modifying shared appearance materials.</summary>
    public sealed class AnimatedTexture : MonoBehaviour
    {
        Renderer target;
        Texture[] frames;
        float minimumSeconds;
        float maximumSeconds;
        int frame;
        MaterialPropertyBlock properties;

        public void Initialize(Renderer target, Texture[] frames, float minimumSeconds, float maximumSeconds)
        {
            properties = new MaterialPropertyBlock();
            this.target = target;
            this.frames = frames;
            this.minimumSeconds = minimumSeconds;
            this.maximumSeconds = maximumSeconds;
            Apply();
            if (isActiveAndEnabled) StartCoroutine(Play());
        }

        void OnEnable() { if (frames != null) StartCoroutine(Play()); }
        void OnDisable() => StopAllCoroutines();
        IEnumerator Play()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minimumSeconds, maximumSeconds));
                frame = (frame + 1) % frames.Length;
                Apply();
            }
        }
        void Apply()
        {
            target.GetPropertyBlock(properties);
            properties.SetTexture("_BaseMap", frames[frame]);
            target.SetPropertyBlock(properties);
        }
    }
}
