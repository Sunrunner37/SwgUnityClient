using UnityEngine;

namespace Swg.Client.Runtime.World
{
    // Scene references are supplied before the game-owned rendering services are resolved.
    public sealed class WorldRenderingSettings
    {
        public Shader WorldShader { get; private set; }
        public Shader CharacterShader { get; private set; }
        public WorldStreamingSettings Streaming { get; private set; }
        public WorldSceneObjectRenderer Renderer { get; private set; }
        public MonoBehaviour Owner { get; private set; }

        public void Initialize(Shader worldShader, Shader characterShader,
            WorldStreamingSettings streaming, WorldSceneObjectRenderer renderer, MonoBehaviour owner)
        {
            WorldShader = worldShader;
            CharacterShader = characterShader;
            Streaming = streaming;
            Renderer = renderer;
            Owner = owner;
        }
    }
}
