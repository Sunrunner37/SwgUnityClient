using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    // Scene references are supplied before the game-owned rendering services are resolved.
    public sealed class WorldRenderingContext
    {
        public Camera WorldCamera { get; set; }
        public Shader WorldShader { get; private set; }
        public Shader ParticleShader { get; private set; }
        public Shader CharacterShader { get; private set; }
        public WorldStreamingSettings Streaming { get; private set; }
        public WorldFloraRenderer FloraRenderer { get; private set; }
        public WorldSceneObjectRenderer Renderer { get; private set; }
        public MonoBehaviour Owner { get; private set; }

        public void Initialize(Shader worldShader, Shader characterShader, Shader particleShader, WorldStreamingSettings streaming, WorldSceneObjectRenderer renderer, MonoBehaviour owner, WorldFloraRenderer floraRenderer = null)
        {
            WorldShader = worldShader;
            CharacterShader = characterShader;
            ParticleShader = particleShader;
            Streaming = streaming;
            Renderer = renderer;
            Owner = owner;
            FloraRenderer = floraRenderer;
        }
    }
}
