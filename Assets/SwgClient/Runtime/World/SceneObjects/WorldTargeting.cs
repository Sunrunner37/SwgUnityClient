using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using System;
using System.Collections;
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
        readonly RaycastHit[] hits = new RaycastHit[64];
        SwgId lastClicked;
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
            var mouse = Mouse.current;
            bool overUi = UiInput.PointerOverUi || UiInput.TextInputFocused || !Application.isFocused;
            SwgId hovered = default;
            if (mouse != null && !overUi && !mouse.rightButton.isPressed)
            {
                var ray = worldCamera.ScreenPointToRay(mouse.position.ReadValue());
                Physics.SyncTransforms();
                int count = Physics.RaycastNonAlloc(ray, hits, maximumDistance, targetMask, QueryTriggerInteraction.Collide);
                float closest = maximumDistance;
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    var candidate = hit.collider.GetComponentInParent<TargetableSceneObject>();
                    if (candidate != null && candidate.SwgId != currentPlayer.SwgId && hit.distance < closest)
                    {
                        closest = hit.distance;
                        hovered = candidate.SwgId;
                    }
                }
                // Terrain hides targets behind a hill. Pick volumes themselves are on a separate layer.
                if (!hovered.IsNone && Physics.Raycast(ray, out var obstruction, closest, ~targetMask, QueryTriggerInteraction.Ignore))
                {
                    var owner = obstruction.collider.GetComponentInParent<ManagedSceneObject>();
                    if (owner == null || owner.SwgId != hovered) hovered = default;
                }
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
            if (!contextMenu.IsOpen && !UiInput.TextInputFocused && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                targeting.Select(default);
        }

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
