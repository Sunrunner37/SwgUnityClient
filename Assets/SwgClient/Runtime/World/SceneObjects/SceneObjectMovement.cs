using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
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
        WorldState state;
        SpatialSceneObject sceneObject;
        SwgId swgId;
        public SwgId SwgId => swgId; // surface to editor

        public void Initialize(SpatialSceneObject sceneObject)
        {
            this.state = GameServices.Resolve<WorldState>();
            this.sceneObject = sceneObject;
            swgId = this.sceneObject.SwgId;
            this.state.SceneObjects.Subscribe(swgId, OnObjectChanged);
            UpdatePose(false);
        }

        void OnObjectChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Replaced) != 0)
                this.state.SceneObjects.TryGet<SpatialSceneObject>(swgId, out this.sceneObject);
            if ((changes & (SceneObjectChange.Transform | SceneObjectChange.Containment | SceneObjectChange.Replaced)) != 0)
                UpdatePose((changes & SceneObjectChange.Containment) != 0);
        }

        void UpdatePose(bool snap)
        {
            if (this.sceneObject == null)
            {
                enabled = false;
                return;
            }

            if (sceneObject is CreatureSceneObject rider && rider.Mount != null)
            {
                enabled = false; // VehicleRider follows the rendered seat instead of interpolating the rider independently.
                return;
            }

            SetTarget(GetLocalPose(state, sceneObject), sceneObject.Scale, snap);
        }

        internal static Pose GetLocalPose(WorldState state, SpatialSceneObject sceneObject)
        {
            var pose = state.SceneObjects.GetServerWorldPose(sceneObject.SwgId);
            // Server terrain and the rendered terrain can disagree. Keep exterior creatures,
            // including summoned vehicles, above our ground without changing network state.
            if (sceneObject is CreatureSceneObject && sceneObject.ParentSwgId.IsNone && state.Terrain != null)
                pose.position.y = Mathf.Max(pose.position.y, state.Terrain.SampleHeight(pose.position.x, pose.position.z));
            return new Pose(pose.position - state.RenderOrigin, pose.rotation);
        }

        internal void Unbind()
        {
            this.state?.SceneObjects.Unsubscribe(swgId, OnObjectChanged);
            this.state = null;
            this.sceneObject = null;
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
