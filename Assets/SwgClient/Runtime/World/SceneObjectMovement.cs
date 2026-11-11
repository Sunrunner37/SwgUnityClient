using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    /// <summary>Interpolates visual poses without changing authoritative SceneObject state.</summary>
    [DisallowMultipleComponent]
    public sealed class SceneObjectMovement : MonoBehaviour
    {
        [Min(0)]
        public float interpolationSeconds = 0.1f;
        [Min(0)]
        public float teleportDistance = 32;
        Pose startLocalPose;
        Pose targetLocalPose;
        float elapsed;
        WorldState world;
        SpatialSceneObject state;
        SwgId swgId;
        public SwgId SwgId => swgId; // surface to editor
        public void Initialize(SpatialSceneObject sceneObject)
        {
            world = GameServices.Resolve<WorldState>();
            state = sceneObject;
            swgId = state.SwgId;
            world.SceneObjects.Subscribe(swgId, OnObjectChanged);
            UpdatePose(false);
        }

        void OnObjectChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Replaced) != 0)
                world.SceneObjects.TryGet<SpatialSceneObject>(swgId, out state);
            if ((changes & (SceneObjectChange.Transform | SceneObjectChange.Containment | SceneObjectChange.Replaced)) != 0)
                UpdatePose((changes & SceneObjectChange.Containment) != 0);
        }

        void UpdatePose(bool snap)
        {
            if (state == null)
            {
                enabled = false;
                return;
            }

            var serverWorldPose = world.SceneObjects.GetServerWorldPose(swgId);
            var localPose = new Pose(serverWorldPose.position - world.RenderOrigin, serverWorldPose.rotation);
            SetTarget(localPose, state.Scale, snap);
        }

        internal void Unbind()
        {
            world?.SceneObjects.Unsubscribe(swgId, OnObjectChanged);
            world = null;
            state = null;
            enabled = false;
        }

        void OnDestroy() => Unbind();
        public void SetTarget(Pose localPose, float scale, bool snap)
        {
            transform.localScale = Vector3.one * scale;
            targetLocalPose = localPose;
            startLocalPose = new Pose(transform.localPosition, transform.localRotation);
            elapsed = 0;
            if (snap || interpolationSeconds <= 0 || Vector3.Distance(startLocalPose.position, targetLocalPose.position) >= teleportDistance)
            {
                Apply(targetLocalPose);
                enabled = false;
                return;
            }

            enabled = startLocalPose.position != targetLocalPose.position || Quaternion.Angle(startLocalPose.rotation, targetLocalPose.rotation) > 0.001f;
        }

        void Update() => Advance(Time.deltaTime);
        internal void Advance(float seconds)
        {
            elapsed += seconds;
            float t = interpolationSeconds <= 0 ? 1 : Mathf.Clamp01(elapsed / interpolationSeconds);
            Apply(new Pose(Vector3.Lerp(startLocalPose.position, targetLocalPose.position, t), Quaternion.Slerp(startLocalPose.rotation, targetLocalPose.rotation, t)));
            if (t >= 1)
                enabled = false;
        }

        void Apply(Pose localPose)
        {
            transform.localPosition = localPose.position;
            transform.localRotation = localPose.rotation;
        }
    }
}
