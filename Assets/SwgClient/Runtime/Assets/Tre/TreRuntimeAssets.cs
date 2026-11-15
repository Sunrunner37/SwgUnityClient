using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;

namespace Swg.Client.Runtime.Assets.Tre
{
    // Archive lifetime and decoded texture cache. No screen or UI state lives here.
    public sealed class TreRuntimeAssets : IDisposable
    {
        readonly Task<TreAssetLoader> mounting;
        readonly HashSet<Task> reads = new HashSet<Task>();
        readonly object readGate = new object ();
        readonly Dictionary<(TextureData Source, TextureWrapMode WrapMode, bool Linear), Texture2D> textures = new Dictionary<(TextureData Source, TextureWrapMode WrapMode, bool Linear), Texture2D>();
        readonly Dictionary<TreVisual, Texture2D> visuals = new Dictionary<TreVisual, Texture2D>();
        bool disposed;
        public Task Ready => mounting;
        public TreAssetLoader Loader => mounting.Result;

        public TreRuntimeAssets(ClientSettings settings)
        {
            mounting = Task.Run(() => new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities));
        }

        public Task<T> ReadAsync<T>(Func<TreAssetLoader, T> read)
        {
            lock (readGate)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(TreRuntimeAssets));
                var task = Task.Run(async () => read(await mounting.ConfigureAwait(false)));
                reads.Add(task);
                _ = task.ContinueWith(completed =>
                {
                    if (completed.IsFaulted)
                        _ = completed.Exception;
                    lock (readGate)
                        reads.Remove(completed);
                }, TaskScheduler.Default);
                return task;
            }
        }

        readonly Dictionary<(string Path, TextureWrapMode WrapMode), Texture2D> pathTextures = new Dictionary<(string, TextureWrapMode), Texture2D>();
        public Texture2D LoadTexture(string swgPath, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            var key = (swgPath.Replace('\\', '/').ToLowerInvariant(), wrapMode);
            if (!pathTextures.TryGetValue(key, out var texture))
            {
                texture = Texture(TextureData.Load(Loader, key.Item1), wrapMode);
                pathTextures.Add(key, texture);
            }

            return texture;
        }

        public Texture2D LoadVisual(TreVisual asset)
        {
            if (!visuals.TryGetValue(asset, out var texture))
            {
                texture = Texture(asset.Load(mounting.Result));
                visuals.Add(asset, texture);
            }

            return texture;
        }

        public Texture2D Texture(TextureData source, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool linear = false)
        {
            if (textures.TryGetValue((source, wrapMode, linear), out var texture))
                return texture;
            bool mipmaps = wrapMode == TextureWrapMode.Repeat;
            texture = new Texture2D(source.Width, source.Height, TextureFormat.RGBA32, mipmaps, linear)
            {
                filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear,
                anisoLevel = mipmaps ? 4 : 1,
                wrapMode = wrapMode
            };
            var raw = new byte[source.Pixels.Length];
            for (int y = 0; y < source.Height; y++)
                Buffer.BlockCopy(source.Pixels, y * source.Width * 4, raw, (source.Height - 1 - y) * source.Width * 4, source.Width * 4);
            texture.SetPixelData(raw, 0);
            texture.Apply(mipmaps, true);
            textures.Add((source, wrapMode, linear), texture);
            return texture;
        }

        public void Dispose()
        {
            Task[] active;
            lock (readGate)
            {
                if (disposed)
                    return;
                disposed = true;
                active = reads.ToArray();
            }

            foreach (var texture in textures.Values)
                if (texture != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(texture);
                    else
                        UnityEngine.Object.DestroyImmediate(texture);
                }

            textures.Clear();
            pathTextures.Clear();
            visuals.Clear();
            // Reads may still be decoding while a screen is closed or Play mode exits.
            Task.WhenAll(active.Concat(new Task[] { mounting })).ContinueWith(completed =>
            {
                _ = completed.Exception;
                if (mounting.Status == TaskStatus.RanToCompletion)
                    mounting.Result.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
