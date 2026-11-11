using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Camera-relative walking with local prediction. SOE sends are queued to the connection thread.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Min(0)]
        public float walkSpeed = 2.5f;
        [Min(.2f)]
        public float sendInterval = .25f;
        WorldState state;
        GameConnection connection;
        GameReplication replication;
        SpatialSceneObject player;
        SceneObjectMovement movement;
        Camera worldCamera;
        float nextSend;
        bool pendingMovement;
        bool wasWalking;
        public void Initialize(SpatialSceneObject player)
        {
            this.player = player;
            this.state = GameServices.Resolve<WorldState>();
            connection = GameServices.Resolve<GameConnection>();
            replication = GameServices.Resolve<GameReplication>();
            worldCamera = GameServices.Resolve<WorldRenderingContext>().WorldCamera;
            movement = GetComponent<SceneObjectMovement>();
            worldCamera.GetComponent<WorldOrbitCamera>().target = transform;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var input = Vector2.zero;
            if (keyboard != null && Application.isFocused)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }

            Walk(input, Time.deltaTime);
        }

        internal void Walk(Vector2 input, float seconds)
        {
            bool walking = input.sqrMagnitude > .001f;
            if (walking)
            {
                input = Vector2.ClampMagnitude(input, 1);
                var forward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                var direction = forward * input.y + right * input.x;
                var serverPose = this.state.SceneObjects.GetServerWorldPose(player.SwgId);
                var serverPosition = serverPose.position + direction * (walkSpeed * seconds);
                // Terrain samples and network positions are absolute SWG world coordinates.
                serverPosition.y = this.state.Terrain.SampleHeight(serverPosition.x, serverPosition.z);
                var serverRotation = Quaternion.LookRotation(direction);
                if (!player.ParentSwgId.IsNone)
                {
                    var parent = this.state.SceneObjects.GetServerWorldPose(player.ParentSwgId);
                    var inverse = Quaternion.Inverse(parent.rotation);
                    player.Position = inverse * (serverPosition - parent.position);
                    player.Rotation = inverse * serverRotation;
                }
                else
                {
                    player.Position = serverPosition;
                    player.Rotation = serverRotation;
                }

                this.state.SceneObjects.NotifyChanged(player, SceneObjectChange.Transform);
                movement.SetTarget(new Pose(serverPosition - this.state.RenderOrigin, serverRotation), player.Scale, true);
                pendingMovement = true;
            }

            if (!walking && wasWalking)
                pendingMovement = true;
            if (pendingMovement && Time.unscaledTime >= nextSend)
            {
                connection.Send(new DataTransformPacket(player.SwgId, player.ParentSwgId, (uint)(Time.realtimeSinceStartupAsDouble * 1000), replication.NextPlayerMovementCounter(), player.Position, player.Rotation, walking ? walkSpeed : 0));
                pendingMovement = false;
                nextSend = Time.unscaledTime + sendInterval;
            }

            wasWalking = walking;
        }
    }
}
