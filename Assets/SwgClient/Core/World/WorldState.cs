using System;
using Swg.Client.Core.Commands;
using UnityEngine;
using Swg.Client.Core.World.Terrain;

namespace Swg.Client.Core.World
{
    /// <summary>Stable session-owned state. Native world assets are populated during world entry.</summary>
    public sealed class WorldState
    {
        public SceneObjectRegistry SceneObjects { get; }
        public PlayerCommands Commands { get; } = new PlayerCommands();
        public SwgId PlayerSwgId { get; private set; }
        public event Action SceneStarted;
        /// <summary>SWG world position mapped to the Unity render origin.</summary>
        public Vector3 RenderOrigin { get; private set; }
        public string TerrainPath { get; private set; }
        public WorldSnapshot Snapshot { get; internal set; }
        public WorldTerrain Terrain { get; internal set; }
        public ObjectTemplateCatalog Templates { get; internal set; }

        public WorldState(SceneObjectRegistry sceneObjects) => SceneObjects = sceneObjects;
        internal void BeginScene(SwgId playerSwgId, Vector3 renderOrigin, string terrainPath)
        {
            SceneObjects.Clear();
            Commands.Apply(list => list.Clear());
            PlayerSwgId = playerSwgId;
            RenderOrigin = renderOrigin;
            TerrainPath = terrainPath;
            Snapshot = null;
            Terrain = null;
            Templates = null;
            SceneStarted?.Invoke();
        }
    }
}
