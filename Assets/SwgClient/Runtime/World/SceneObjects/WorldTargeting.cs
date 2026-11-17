using Swg.Client.Core.Characters;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>World mouse input. UI and camera drag retain priority over targeting.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldTargeting : MonoBehaviour
    {
        public RadialContextMenu contextMenu;
        public float holdSeconds = .35f;
        public float holdMovementTolerance = 8;
        SwgId heldSwgId;
        Vector2 holdPosition;
        float holdStart;
        public Camera worldCamera;
        public TreTexture hoverCursor;
        public Vector2 cursorHotspot = new Vector2(16, 16);
        public float maximumDistance = 256;
        readonly List<(SwgId Id, float Distance)> cycleTargets = new List<(SwgId, float)>();
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly RaycastHit[] obstructions = new RaycastHit[64];
        SwgId lastClicked;
        SwgId lastCycled;
        float lastClickTime = float.NegativeInfinity;
        ClientObjectInteractions interactions;
        ClientTargeting targeting;
        WorldState state;
        CurrentPlayer currentPlayer;
        Texture2D cursor;
        ClientCursor cursors;
        int targetMask;

        void Awake()
        {
            targeting = GameServices.Resolve<ClientTargeting>();
            cursors = GameServices.Resolve<ClientCursor>();
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            targetMask = LayerMask.GetMask(TargetableSceneObject.LayerName);
        }

        void OnEnable()
        {
            if (hoverCursor != null && cursor == null)
                StartCoroutine(LoadCursor());
        }

        IEnumerator LoadCursor()
        {
            var read = GameServices.Resolve<TreRuntimeAssets>().ReadAsync(loader => hoverCursor.Load(loader));
            yield return new WaitUntil(() => read.IsCompleted);
            if (read.IsFaulted)
            {
                Debug.LogException(read.Exception, this);
                yield break;
            }
            var source = read.Result;
            cursor = ClientCursor.Create(source);
        }

        void LateUpdate()
        {
            if (Application.isFocused && !UiInput.TextInputFocused && !contextMenu.IsOpen && Keyboard.current?.tabKey.wasPressedThisFrame == true)
                CycleTarget();
            var mouse = Mouse.current;
            bool overUi = UiInput.PointerOverUi || UiInput.TextInputFocused || !Application.isFocused;
            SwgId hovered = default;
            if (mouse != null && !overUi && !mouse.rightButton.isPressed)
            {
                hovered = Pick(worldCamera.ScreenPointToRay(mouse.position.ReadValue()));
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    targeting.Select(hovered);
                    heldSwgId = hovered;
                    holdPosition = mouse.position.ReadValue();
                    holdStart = Time.unscaledTime;
                    if (!hovered.IsNone && hovered == lastClicked && Time.unscaledTime - lastClickTime < .3f)
                    {
                        interactions.ActivateDefault(hovered);
                        heldSwgId = default;
                    }
                    lastClicked = hovered; lastClickTime = Time.unscaledTime;
                }
                if (!heldSwgId.IsNone && mouse.leftButton.isPressed && hovered == heldSwgId && Vector2.Distance(holdPosition, mouse.position.ReadValue()) <= holdMovementTolerance && Time.unscaledTime - holdStart >= holdSeconds)
                {
                    contextMenu.Open(heldSwgId, holdPosition);
                    heldSwgId = default;
                }
            }
            if (mouse == null || !mouse.leftButton.isPressed || overUi || hovered != heldSwgId || Vector2.Distance(holdPosition, mouse.position.ReadValue()) > holdMovementTolerance)
                heldSwgId = default;
            targeting.Hover(hovered);
            state.SceneObjects.TryGet(hovered, out var hoveredObject);
            var cursorKind = ClientCursor.ForObject(hoveredObject, interactions.ObjectType(hoveredObject), !hovered.IsNone && interactions.HasDefaultAction(hovered));
            cursors.SetWorldHover(!hovered.IsNone, cursor, cursorHotspot, cursorKind);
            if (!contextMenu.IsOpen && !UiInput.TextInputFocused && UiInput.TryConsumeEscape())
                targeting.Select(default);
        }

        internal void CycleTarget()
        {
            cycleTargets.Clear();
            var position = state.SceneObjects.GetServerWorldPose(currentPlayer.SwgId).position;
            foreach (var sceneObject in state.SceneObjects)
            {
                if (sceneObject is not CreatureSceneObject creature || creature.SwgId == currentPlayer.SwgId
                    || !creature.CanAttack || creature.Posture == CreaturePosture.Dead || creature.Posture == CreaturePosture.Incapacitated
                    || !ManagedSceneObject.TryGetObjectFrom(creature.SwgId, out var body)
                    || !body.TryGetComponent<TargetableSceneObject>(out _)) continue;
                float distance = (state.SceneObjects.GetServerWorldPose(creature.SwgId).position - position).sqrMagnitude;
                if (distance <= maximumDistance * maximumDistance) cycleTargets.Add((creature.SwgId, distance));
            }
            cycleTargets.Sort((left, right) => {
                int distance = left.Distance.CompareTo(right.Distance);
                return distance != 0 ? distance : left.Id.Value.CompareTo(right.Id.Value);
            });
            if (cycleTargets.Count == 0) return;
            int selected = targeting.SelectedSwgId == lastCycled ? cycleTargets.FindIndex(candidate => candidate.Id == lastCycled) : -1;
            lastCycled = cycleTargets[(selected + 1) % cycleTargets.Count].Id;
            targeting.Select(lastCycled);
        }

        internal SwgId Pick(Ray ray)
        {
            SwgId hovered = default;
            Physics.SyncTransforms();
            int count = Physics.RaycastNonAlloc(ray, hits, maximumDistance, targetMask, QueryTriggerInteraction.Collide);
            float closest = maximumDistance;
            Collider selectedVolume = null;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var candidate = hit.collider.GetComponentInParent<TargetableSceneObject>();
                if (candidate == null || candidate.SwgId == currentPlayer.SwgId) continue;
                bool performer = selectedVolume != null && PerformerOverInstrument(candidate.SwgId, hit.collider.bounds, hovered, selectedVolume.bounds);
                bool instrument = selectedVolume != null && PerformerOverInstrument(hovered, selectedVolume.bounds, candidate.SwgId, hit.collider.bounds);
                if (performer || hit.distance < closest && !instrument)
                {
                    closest = hit.distance;
                    hovered = candidate.SwgId;
                    selectedVolume = hit.collider;
                }
            }
            // Ignore an overlapping performance instrument, but still test the rest of the ray for walls/terrain.
            if (!hovered.IsNone)
            {
                int blocking = Physics.RaycastNonAlloc(ray, obstructions, closest, ~targetMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < blocking; i++)
                {
                    var obstruction = obstructions[i].collider;
                    var owner = obstruction.GetComponentInParent<ManagedSceneObject>();
                    if (owner != null && (owner.SwgId == hovered || PerformerOverInstrument(hovered, selectedVolume.bounds, owner.SwgId, obstruction.bounds))) continue;
                    hovered = default;
                    break;
                }
            }
            return hovered;
        }

        internal bool PerformerOverInstrument(SwgId performer, Bounds performerBounds, SwgId instrument, Bounds instrumentBounds)
            => state.SceneObjects.TryGet<CreatureSceneObject>(performer, out var creature) && creature.IsPlayingMusic
                && state.SceneObjects.TryGet(instrument, out var item) && state.Templates.GameObjectType(item.TemplatePath) == 0x200c
                && performerBounds.Intersects(instrumentBounds);

        void OnDisable()
        {
            targeting.Hover(default);
            cursors.SetWorldHover(false, cursor, cursorHotspot);
            StopAllCoroutines();
        }

        void OnDestroy()
        {
            if (cursor != null)
                Destroy(cursor);
        }
    }
}
