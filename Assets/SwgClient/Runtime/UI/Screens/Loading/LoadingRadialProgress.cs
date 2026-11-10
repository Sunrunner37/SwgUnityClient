using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Screens.Loading
{
    /// <summary>Native UIPie geometry: a textured annulus built from the original radial strip.</summary>
    [ExecuteAlways]
    public sealed class LoadingRadialProgress : RawImage
    {
        [Range(0, 1)]
        public float value = .5f;
        public float startAngle = 1;
        public float segmentAngle = .2f;
        public float thickness = 33;
        public void SetValue(float progress)
        {
            float next = Mathf.Clamp01(progress);
            if (Mathf.Approximately(value, next))
                return;
            value = next;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            float outer = Mathf.Min(rect.width, rect.height) * .5f;
            float inner = Mathf.Max(0, outer - thickness);
            float arc = Mathf.Clamp01(value) * Mathf.PI * 2;
            float step = Mathf.Max(.02f, segmentAngle);
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            for (float offset = 0; offset < arc; offset += step)
            {
                float end = Mathf.Min(arc, offset + step);
                int first = helper.currentVertCount;
                Add(outer, startAngle - offset, new Vector2(0, 1));
                Add(inner, startAngle - offset, new Vector2(0, 0));
                Add(inner, startAngle - end, new Vector2((end - offset) / step, 0));
                Add(outer, startAngle - end, new Vector2((end - offset) / step, 1));
                helper.AddTriangle(first, first + 1, first + 2);
                helper.AddTriangle(first, first + 2, first + 3);
            }

            void Add(float radius, float angle, Vector2 uv)
            {
                vertex.position = rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.uv0 = uv;
                helper.AddVert(vertex);
            }
        }
    }
}
