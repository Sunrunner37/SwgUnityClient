using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swg.Client.Core.Audio;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ClientMusic : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Task<AudioClip>> pending = new Dictionary<string, Task<AudioClip>>(StringComparer.OrdinalIgnoreCase);
        int requestVersion;
        AudioSource Source => GetComponent<AudioSource>();
        public float Volume
        {
            set => Source.volume = value;
        }

        public Task<AudioClip> LoadAsync(TreRuntimeAssets assets, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A virtual music asset path is required.", nameof(path));
            path = path.Replace('\\', '/');
            if (clips.TryGetValue(path, out var clip))
                return Task.FromResult(clip);
            if (!pending.TryGetValue(path, out var task))
            {
                task = DecodeAsync(assets, path);
                pending.Add(path, task);
            }

            return task;
        }

        async Task<AudioClip> DecodeAsync(TreRuntimeAssets assets, string path)
        {
            try
            {
                var pcm = await assets.ReadAsync(loader => MusicLoader.Load(loader, path));
                if (this == null)
                    throw new OperationCanceledException("Music player was disposed.");
                var clip = AudioClip.Create(path, pcm.Samples.Length / pcm.Channels, pcm.Channels, pcm.Rate, false);
                clip.SetData(pcm.Samples, 0);
                clips.Add(path, clip);
                return clip;
            }
            finally
            {
                pending.Remove(path);
            }
        }

        public void Request(TreRuntimeAssets assets, string path, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (Source.clip != null && (!clips.TryGetValue(path, out var clip) || Source.clip != clip))
                Source.Stop();
            int version = ++requestVersion;
            _ = PlayWhenReady(assets, path, loop, version);
        }

        async Task PlayWhenReady(TreRuntimeAssets assets, string path, bool loop, int version)
        {
            try
            {
                var clip = await LoadAsync(assets, path);
                if (this != null && version == requestVersion)
                    PlayClip(clip, loop);
            }
            catch (Exception e)
            {
                if (this != null && version == requestVersion)
                    Debug.LogWarning("Music unavailable (" + path + "): " + e.Message);
            }
        }

        public void Play(AudioClip clip, bool loop)
        {
            requestVersion++;
            PlayClip(clip, loop);
        }

        void PlayClip(AudioClip clip, bool loop)
        {
            if (Source.clip == clip && Source.isPlaying)
            {
                Source.loop = loop;
                return;
            }

            Source.Stop();
            Source.clip = clip;
            Source.spatialBlend = 0;
            Source.loop = loop;
            Source.Play();
        }

        void OnDestroy()
        {
            requestVersion++;
            if (Source != null)
                Source.Stop();
            foreach (var clip in clips.Values)
                if (clip != null)
                    Destroy(clip);
        }
    }
}
