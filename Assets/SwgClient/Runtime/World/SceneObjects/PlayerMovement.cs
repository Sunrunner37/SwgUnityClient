using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Appearances;
using UnityEngine;
using Swg.Client.Runtime.UI;
using UnityEngine.InputSystem;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Camera-relative running and prone crawling with local prediction. SOE sends are queued to the connection thread.</summary>
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
        ClientCommands commands;
        bool standRequested;
        CreatureSceneObject player;
        SceneObjectMovement movement;
        CharacterAnimation animation;
        Camera worldCamera;
        float nextSend;
        bool pendingMovement;
        bool wasMoving;
        CreatureSceneObject lastMount;
        float vehicleAcceleration;
        public void Initialize(SpatialSceneObject player)
        {
            this.player = (CreatureSceneObject)player;
            this.state = GameServices.Resolve<WorldState>();
            connection = GameServices.Resolve<GameConnection>();
            replication = GameServices.Resolve<GameReplication>();
            commands = GameServices.Resolve<ClientCommands>();
            worldCamera = GameServices.Resolve<WorldRenderingContext>().WorldCamera;
            movement = GetComponent<SceneObjectMovement>();
            animation = GetComponent<CharacterAnimation>();
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

            Move(input, Time.deltaTime);
        }

        internal static float AdvanceVehicleSpeed(float current, float target, float acceleration, float seconds)
            => Mathf.MoveTowards(current, target, Mathf.Max(0, acceleration) * Mathf.Max(0, seconds));

        internal void Move(Vector2 input, float seconds)
        {
            var controlled = player.Mount ?? player;
            bool mounted = player.Mount != null;
            if (player.Mount != lastMount)
            {
                lastMount = player.Mount;
                CurrentSpeed = 0;
                vehicleAcceleration = mounted ? state.Templates.CreatureMovement(controlled.TemplatePath).RunAcceleration : 0;
            }
            bool wantsMovement = input.sqrMagnitude > .001f;
            if (!wantsMovement || player.Posture == CreaturePosture.Upright)
                standRequested = false;
            else if (wantsMovement && !standRequested && (player.Posture == CreaturePosture.Sitting || player.Posture == CreaturePosture.Crouched))
            {
                standRequested = true;
                commands.Enqueue("stand");
            }
            bool canMove = player.MovementSpeed > 0 && (player.Posture == CreaturePosture.Upright || player.Posture == CreaturePosture.Prone) && (animation == null || !animation.IsChangingPosture);
            bool moving = canMove && (wantsMovement || mounted && CurrentSpeed > 0);
            if (!moving) CurrentSpeed = 0;
            if (moving && seconds > 0)
            {
                input = Vector2.ClampMagnitude(input, 1);
                var forward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                var direction = forward * input.y + right * input.x;
                var serverPose = this.state.SceneObjects.GetServerWorldPose(controlled.SwgId);
                var desiredRotation = wantsMovement ? Quaternion.LookRotation(direction) : serverPose.rotation;
                var serverRotation = Quaternion.RotateTowards(serverPose.rotation, desiredRotation, turnDegreesPerSecond * player.TurnScale * seconds);
                var heading = serverRotation * Vector3.forward;
                // Move along the facing direction through turns instead of sliding sideways.
                float alignment = wantsMovement ? Mathf.Clamp01(Vector3.Dot(heading, direction.normalized)) : 0;
                float targetSpeed = player.MovementSpeed * input.magnitude * alignment;
                float previousSpeed = CurrentSpeed;
                CurrentSpeed = mounted
                    ? AdvanceVehicleSpeed(CurrentSpeed, targetSpeed, vehicleAcceleration * player.AccelerationMultiplierBase * player.AccelerationMultiplier, seconds)
                    : targetSpeed;
                float distance = (mounted ? (previousSpeed + CurrentSpeed) * .5f : CurrentSpeed) * seconds;
                var serverPosition = serverPose.position + heading * distance;
                // Terrain samples and network positions are absolute SWG world coordinates.
                serverPosition.y = this.state.Terrain.SampleHeight(serverPosition.x, serverPosition.z);
                if (!mounted && !player.ParentSwgId.IsNone)
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

                controlled.Position = mounted ? serverPosition : player.Position;
                controlled.Rotation = mounted ? serverRotation : player.Rotation;
                this.state.SceneObjects.NotifyChanged(controlled, SceneObjectChange.Transform);
                if (mounted)
                {
                    if (ManagedSceneObject.TryGetObjectFrom(controlled.SwgId, out var vehicle) && vehicle.TryGetComponent<SceneObjectMovement>(out var vehicleMovement))
                        vehicleMovement.SetTarget(new Pose(serverPosition - this.state.RenderOrigin, serverRotation), controlled.Scale, true);
                }
                else
                    movement.SetTarget(new Pose(serverPosition - this.state.RenderOrigin, serverRotation), player.Scale, true);
                pendingMovement = true;
            }

            bool stopped = CurrentSpeed <= 0 && wasMoving;
            if (stopped)
                pendingMovement = true;
            if (pendingMovement && (stopped || Time.unscaledTime >= nextSend))
            {
                connection.Send(new DataTransformPacket(player.SwgId, mounted ? default : player.ParentSwgId, (uint)(Time.realtimeSinceStartupAsDouble * 1000), replication.NextPlayerMovementCounter(), player.Position, player.Rotation, CurrentSpeed));
                pendingMovement = false;
                nextSend = Time.unscaledTime + sendInterval;
            }

            wasMoving = CurrentSpeed > 0;
        }
    }
}
