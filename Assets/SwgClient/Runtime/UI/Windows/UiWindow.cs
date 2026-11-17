using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swg.Client.Runtime.UI.Windows
{
    [Flags]
    public enum WindowEdges
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8
    }

    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class UiWindow : MonoBehaviour
    {
        internal static readonly List<UiWindow> OpenWindows = new List<UiWindow>();
        static long nextFocus;
        internal long FocusOrder { get; private set; }
        public Button closeButton;
        void OnEnable() { OpenWindows.Add(this); FocusOrder = ++nextFocus; }
        void OnDisable() => OpenWindows.Remove(this);
        internal void Focus()
        {
            FocusOrder = ++nextFocus;
            transform.SetAsLastSibling();
        }

        public Vector2 minimumSize = new Vector2(296, 120);
        RectTransform Parent => (RectTransform)transform.parent;

        Vector2 pointerStart;
        Vector2 minimumStart;
        Vector2 maximumStart;
        readonly Vector3[] corners = new Vector3[4];
        public void BeginDrag(PointerEventData pointer)
        {
            Focus();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, pointer.position, pointer.pressEventCamera, out pointerStart);
            ((RectTransform)transform).GetWorldCorners(corners);
            minimumStart = Parent.InverseTransformPoint(corners[0]);
            maximumStart = Parent.InverseTransformPoint(corners[2]);
        }

        public void Drag(PointerEventData pointer, WindowEdges edges)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, pointer.position, pointer.pressEventCamera, out var position))
                return;
            Resize(position - pointerStart, edges);
        }

        internal void Resize(Vector2 delta, WindowEdges edges)
        {
            var min = minimumStart;
            var max = maximumStart;
            var bounds = Parent.rect;
            var size = Vector2.Min(minimumSize, bounds.size);
            if (edges == WindowEdges.None)
            {
                var movement = new Vector2(Mathf.Clamp(delta.x, bounds.xMin - min.x, bounds.xMax - max.x), Mathf.Clamp(delta.y, bounds.yMin - min.y, bounds.yMax - max.y));
                min += movement;
                max += movement;
            }
            else
            {
                if ((edges & WindowEdges.Left) != 0)
                    min.x = Mathf.Clamp(min.x + delta.x, bounds.xMin, max.x - size.x);
                if ((edges & WindowEdges.Right) != 0)
                    max.x = Mathf.Clamp(max.x + delta.x, min.x + size.x, bounds.xMax);
                if ((edges & WindowEdges.Bottom) != 0)
                    min.y = Mathf.Clamp(min.y + delta.y, bounds.yMin, max.y - size.y);
                if ((edges & WindowEdges.Top) != 0)
                    max.y = Mathf.Clamp(max.y + delta.y, min.y + size.y, bounds.yMax);
            }

            var rect = (RectTransform)transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max.x - min.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, max.y - min.y);
            rect.GetWorldCorners(corners);
            var current = (Vector2)Parent.InverseTransformPoint(corners[0]);
            rect.position += Parent.TransformVector(min - current);
        }
    }
}
