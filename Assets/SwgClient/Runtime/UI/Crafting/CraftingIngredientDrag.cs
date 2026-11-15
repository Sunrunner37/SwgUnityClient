using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Swg.Client.Runtime.UI.Crafting
{
    public sealed class CraftingIngredientDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RawImage icon;
        public Action Select;
        RectTransform ghost;
        public void OnBeginDrag(PointerEventData pointer)
        {
            Select();
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            ghost = new GameObject("Crafting ingredient drag", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RectTransform>();
            ghost.SetParent(canvas.transform, false);
            ghost.sizeDelta = new Vector2(52, 52);
            var image = ghost.GetComponent<RawImage>();
            image.texture = icon.texture;
            image.uvRect = icon.uvRect;
            image.raycastTarget = false;
            image.color = new Color(1, 1, 1, .8f);
            OnDrag(pointer);
        }
        public void OnDrag(PointerEventData pointer)
        {
            if (ghost == null) return;
            var canvas = ghost.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, pointer.position, canvas.worldCamera, out var position);
            ghost.anchoredPosition = position;
        }
        public void OnEndDrag(PointerEventData pointer) => Clear();
        void OnDisable() => Clear();
        void Clear() { if (ghost != null) Destroy(ghost.gameObject); }
    }
}
