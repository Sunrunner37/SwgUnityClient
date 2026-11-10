using System;
using System.Collections;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ClientMusic : MonoBehaviour
    {
        AudioSource source;
        AudioClipLoader clips;
        Coroutine pendingPlayback;
        void Awake() => source = GetComponent<AudioSource>();
        public float Volume { set => source.volume = value; }

        public void Request(string path, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            StopPendingPlayback();
            clips = clips ?? GameServices.Resolve<AudioClipLoader>();
            // Start with a frame boundary so even a cached clip cannot complete before the handle is assigned.
            pendingPlayback = StartCoroutine(PlayWhenReady(path, loop));
        }

        IEnumerator PlayWhenReady(string path, bool loop)
        {
            yield return null;
            var task = clips.LoadAsync(path);
            yield return new WaitUntil(() => task.IsCompleted);
            pendingPlayback = null;
            if (task.IsFaulted)
            {
                Debug.LogWarning("Music unavailable (" + path + "): " + task.Exception.GetBaseException().Message, this);
                yield break;
            }

            if (!task.IsCanceled)
                PlayClip(task.Result, loop);
        }

        public void Play(AudioClip clip, bool loop)
        {
            StopPendingPlayback();
            PlayClip(clip, loop);
        }

        void PlayClip(AudioClip clip, bool loop)
        {
            if (source.clip == clip && source.isPlaying)
            {
                source.loop = loop;
                return;
            }

            source.Stop();
            source.clip = clip;
            source.spatialBlend = 0;
            source.loop = loop;
            source.Play();
        }

        void StopPendingPlayback()
        {
            if (pendingPlayback == null)
                return;
            StopCoroutine(pendingPlayback);
            pendingPlayback = null;
        }

        public void Stop()
        {
            StopPendingPlayback();
            source.Stop();
        }

        void OnDestroy()
        {
            StopPendingPlayback();
            if (source != null)
                source.Stop();
        }
    }
}
