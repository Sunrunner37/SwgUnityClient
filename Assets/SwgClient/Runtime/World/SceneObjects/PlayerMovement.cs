using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using UnityEngine;
using Swg.Client.Runtime.UI;
using UnityEngine.InputSystem;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Camera-relative running with local prediction. SOE sends are queued to the connection thread.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Min(0)]
        public float turnDegreesPerSecond = 540;
        public float CurrentSpeed { get; private set; }

        [Min(.2f)]
        public float sendInterval = .25f;
        WorldState state;
        GameConnection connection;
        GameReplication replication;
        CreatureSceneObject player;
        SceneObjectMovement movement;
        Camera worldCamera;
        float nextSend;
        bool pendingMovement;
        bool wasRunning;
        public void Initialize(SpatialSceneObject player)
        {
            this.player = (CreatureSceneObject)player;
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
            if (keyboard != null && Application.isFocused && !UiInput.TextInputFocused)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }

            Run(input, Time.deltaTime);
        }

        internal void Run(Vector2 input, float seconds)
        {
            bool running = input.sqrMagnitude > .001f && player.RunningSpeed > 0;
            CurrentSpeed = 0;
            if (running && seconds > 0)
            {
                input = Vector2.ClampMagnitude(input, 1);
                var forward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                var direction = forward * input.y + right * input.x;
                var serverPose = this.state.SceneObjects.GetServerWorldPose(player.SwgId);
                var desiredRotation = Quaternion.LookRotation(direction);
                var serverRotation = Quaternion.RotateTowards(serverPose.rotation, desiredRotation, turnDegreesPerSecond * player.TurnScale * seconds);
                var heading = serverRotation * Vector3.forward;
                // Run along the facing direction through turns; don't slide sideways or run backwards.
                float alignment = Mathf.Clamp01(Vector3.Dot(heading, direction.normalized));
                CurrentSpeed = player.RunningSpeed * input.magnitude * alignment;
                var serverPosition = serverPose.position + heading * (CurrentSpeed * seconds);
                // Terrain samples and network positions are absolute SWG world coordinates.
                serverPosition.y = this.state.Terrain.SampleHeight(serverPosition.x, serverPosition.z);
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

            bool stopped = !running && wasRunning;
            if (stopped)
                pendingMovement = true;
            if (pendingMovement && (stopped || Time.unscaledTime >= nextSend))
            {
                connection.Send(new DataTransformPacket(player.SwgId, player.ParentSwgId, (uint)(Time.realtimeSinceStartupAsDouble * 1000), replication.NextPlayerMovementCounter(), player.Position, player.Rotation, CurrentSpeed));
                pendingMovement = false;
                nextSend = Time.unscaledTime + sendInterval;
            }

            wasRunning = running;
        }
    }
}
