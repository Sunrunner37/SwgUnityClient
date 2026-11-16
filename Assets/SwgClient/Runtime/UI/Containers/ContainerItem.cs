using System;
using System.Collections;
using UnityEngine.EventSystems;
using Swg.Client.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Containers
{
    public sealed class ContainerItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public Button button;
        public RawImage image;
        public TMP_Text itemName;
        public TMP_Text quantity;
        public GameObject selection;
        public GameObject equipped;
        public GameObject activeIndicator;
        public SwgId SwgId { get; private set; }
        public bool CanActivate { get; set; }
        public ClientCursor.WorldCursor ActivationCursor { get; set; }
        ClientCursor cursors;
        Action doubleClick;
        Action<Vector2> hold;
        Coroutine holding;
        Vector2 pressedAt;
        bool held;
        public void OnPointerDown(PointerEventData pointer)
        {
            if (pointer.button != PointerEventData.InputButton.Left || hold == null) return;
            held = false;
            pressedAt = pointer.position;
            holding = StartCoroutine(Hold());
        }
        IEnumerator Hold()
        {
            yield return new WaitForSecondsRealtime(.35f);
            holding = null;
            held = true;
            hold(pressedAt);
        }
        void CancelHold()
        {
            if (holding != null) StopCoroutine(holding);
            holding = null;
        }
        public void OnPointerUp(PointerEventData pointer) => CancelHold();
        public void OnPointerEnter(PointerEventData pointer)
        {
            if (!CanActivate) return;
            cursors = GameServices.Resolve<ClientCursor>();
            cursors.ShowActivation(this, ActivationCursor);
        }
        public void OnPointerExit(PointerEventData pointer)
        {
            CancelHold();
            cursors?.ClearActivation(this);
        }
        public void OnPointerMove(PointerEventData pointer)
        {
            if (Vector2.Distance(pressedAt, pointer.position) > 8) CancelHold();
        }
        void OnDisable()
        {
            CancelHold();
            cursors?.ClearActivation(this);
        }
        public void OnPointerClick(PointerEventData pointer)
        {
            if (!held && pointer.button == PointerEventData.InputButton.Left && pointer.clickCount == 2) doubleClick?.Invoke();
        }
        public void Bind(SwgId swgId, Action select, Action doubleClick = null, Action<Vector2> hold = null)
        {
            SwgId = swgId;
            this.doubleClick = doubleClick;
            this.hold = hold;
            button.onClick.AddListener(() => select());
        }
    }
}
