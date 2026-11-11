using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World.Terrain
{
    /// <summary>Registers an object's authored client-data clearances for its enabled lifetime.</summary>
    public sealed class FloraExclusion : MonoBehaviour
    {
        WorldState state;
        WorldFloraRenderer flora;
        SwgId swgId;
        FloraClearance[] circles;
        public void Initialize(WorldState state, WorldFloraRenderer flora, SwgId swgId, FloraClearance[] circles)
        {
            this.state = state;
            this.flora = flora;
            this.swgId = swgId;
            this.circles = circles;
            if (isActiveAndEnabled)
                Register();
        }

        void OnEnable()
        {
            if (this.state != null)
                Register();
        }

        void Register()
        {
            this.state.SceneObjects.Subscribe(swgId, OnChanged);
            Refresh();
        }

        void OnChanged(SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Transform | SceneObjectChange.Containment)) != 0)
                Refresh();
        }

        void Refresh()
        {
            if (!this.state.SceneObjects.TryGet<SpatialSceneObject>(swgId, out var sceneObject))
                return;
            float scale = sceneObject.Scale;
            var pose = this.state.SceneObjects.GetServerWorldPose(swgId);
            var transformed = new FloraClearance[circles.Length];
            for (int i = 0; i < circles.Length; i++)
                transformed[i] = new FloraClearance(pose.position - this.state.RenderOrigin + pose.rotation * (circles[i].Center * scale), circles[i].Radius * scale, circles[i].Collidable);
            flora.SetExclusion(swgId, transformed);
        }

        void OnDisable()
        {
            if (this.state == null)
                return;
            this.state.SceneObjects.Unsubscribe(swgId, OnChanged);
            if (flora != null)
                flora.RemoveExclusion(swgId);
        }
    }
}
