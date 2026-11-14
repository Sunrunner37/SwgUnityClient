using System;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.UI
{
    /// <summary>Shares native cursor textures and gives activatable UI items priority over world hover.</summary>
    public sealed class ClientCursor : IDisposable
    {
        readonly TreRuntimeAssets assets;
        Texture2D activation;
        Texture2D world;
        Texture2D shown;
        UnityEngine.Object activationOwner;
        bool worldHovered;
        Vector2 worldHotspot;

        public ClientCursor(TreRuntimeAssets assets) => this.assets = assets;

        public void ShowActivation(UnityEngine.Object owner)
        {
            if (activation == null)
                activation = Create(TextureData.Load(assets.Loader, "texture/ui_cursor_activate.dds"));
            activationOwner = owner;
            Refresh();
        }
        public void ClearActivation(UnityEngine.Object owner)
        {
            if (activationOwner != owner) return;
            activationOwner = null;
            Refresh();
        }
        public void SetWorldHover(bool hovered, Texture2D texture, Vector2 hotspot)
        {
            worldHovered = hovered;
            world = texture;
            worldHotspot = hotspot;
            Refresh();
        }
        void Refresh()
        {
            var texture = activationOwner != null ? activation : worldHovered ? world : null;
            if (texture == shown) return;
            shown = texture;
            Cursor.SetCursor(texture, texture == null ? Vector2.zero : activationOwner != null ? new Vector2(16, 16) : worldHotspot, CursorMode.Auto);
        }
        internal static Texture2D Create(TextureData source)
        {
            var texture = new Texture2D(source.Width, source.Height, TextureFormat.RGBA32, false);
            var pixels = new byte[source.Pixels.Length];
            for (int y = 0; y < source.Height; y++)
                Buffer.BlockCopy(source.Pixels, y * source.Width * 4, pixels, (source.Height - 1 - y) * source.Width * 4, source.Width * 4);
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, false); // OS cursors require readable textures.
            return texture;
        }
        public void Dispose()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            if (activation != null) UnityEngine.Object.Destroy(activation);
        }
    }
}