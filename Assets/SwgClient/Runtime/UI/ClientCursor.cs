using System;
using System.Collections.Generic;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Networking;
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
        readonly Dictionary<WorldCursor, Texture2D> worldTextures = new Dictionary<WorldCursor, Texture2D>();
        public enum WorldCursor { Default, Examine, Attack, Talk, Use }

        public static WorldCursor ForObject(SceneObject sceneObject, int objectType, bool useAction)
        {
            if (ObjectMenuDefaults.IsVehicle(objectType)) return WorldCursor.Examine;
            if (sceneObject is TangibleSceneObject tangible)
            {
                if (tangible.CanConverse) return WorldCursor.Talk;
                if (tangible.CanAttack && !(sceneObject is CreatureSceneObject creature && creature.Posture == CreaturePosture.Dead)) return WorldCursor.Attack;
            }
            return useAction ? WorldCursor.Use : WorldCursor.Examine;
        }
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
        public void SetWorldHover(bool hovered, Texture2D texture, Vector2 hotspot, WorldCursor kind = WorldCursor.Default)
        {
            worldHovered = hovered;
            world = texture;
            if (hovered && kind != WorldCursor.Default)
            {
                if (!worldTextures.TryGetValue(kind, out world))
                {
                    string name = kind == WorldCursor.Examine ? "info" : kind.ToString().ToLowerInvariant();
                    world = Create(TextureData.Load(assets.Loader, "texture/ui_cursor_" + name + ".dds"));
                    worldTextures.Add(kind, world);
                }
            }
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
            foreach (var texture in worldTextures.Values) UnityEngine.Object.Destroy(texture);
        }
    }
}