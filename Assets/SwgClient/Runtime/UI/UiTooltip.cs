using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swg.Client.Runtime.UI
{
    public sealed class UiTooltip : MonoBehaviour
    {
        public TMP_Text label;
        public float maximumWidth = 520;
        public void Show(string text, PointerEventData pointer)
        {
            label.text = text;
            var size = label.GetPreferredValues(text, maximumWidth - 16, 0);
            ((RectTransform)transform).sizeDelta = new Vector2(Mathf.Min(maximumWidth, size.x + 16), size.y + 12);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Move(pointer);
        }
        public void Move(PointerEventData pointer)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var bounds = (RectTransform)canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, pointer.position, canvas.worldCamera, out var position);
            var size = ((RectTransform)transform).rect.size;
            position += new Vector2(12, -14);
            position.x = Mathf.Clamp(position.x, bounds.rect.xMin, bounds.rect.xMax - size.x);
            position.y = Mathf.Clamp(position.y, bounds.rect.yMin + size.y, bounds.rect.yMax);
            transform.position = bounds.TransformPoint(position);
        }
        public void Hide() => gameObject.SetActive(false);
    }
}
