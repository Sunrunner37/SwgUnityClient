using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Audio;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Audio
{
    /// <summary>Loads and owns shared audio clips. Call from the Unity main thread.</summary>
    public sealed class AudioClipLoader : IDisposable
    {
        readonly TreRuntimeAssets assets;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Task<AudioClip>> pending = new Dictionary<string, Task<AudioClip>>(StringComparer.OrdinalIgnoreCase);
        bool disposed;
        public AudioClipLoader(TreRuntimeAssets assets) => this.assets = assets;
        public Task<AudioClip> LoadAsync(string path)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(AudioClipLoader));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A virtual audio asset path is required.", nameof(path));
            path = path.Replace('\\', '/');
            if (clips.TryGetValue(path, out var clip))
                return Task.FromResult(clip);
            if (!pending.TryGetValue(path, out var task))
            {
                task = DecodeAsync(path);
                pending.Add(path, task);
                _ = task.ContinueWith(completed =>
                {
                    _ = completed.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }

            return task;
        }

        async Task<AudioClip> DecodeAsync(string path)
        {
            try
            {
                var pcm = await assets.ReadAsync(loader => MusicLoader.Load(loader, path));
                if (disposed)
                    throw new OperationCanceledException("Audio clip loader was disposed.");
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

        public void Dispose()
        {
            disposed = true;
            foreach (var clip in clips.Values)
                if (clip != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(clip);
                    else
                        UnityEngine.Object.DestroyImmediate(clip);
                }

            clips.Clear();
        }
    }
}
