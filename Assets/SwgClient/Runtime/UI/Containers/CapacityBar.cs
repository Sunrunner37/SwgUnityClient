using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Containers
{
    /// <summary>Two rows of capacity blocks, aligned to display pixels instead of stretching a bitmap.</summary>
    public sealed class CapacityBar : MaskableGraphic
    {
        [SerializeField, Range(0, 1)] float fill = 1;
        public Color backgroundColor = new Color(0, .035f, .04f, 1);
        [Min(1)] public int blockWidth = 5;
        [Min(1)] public int blockHeight = 5;
        [Min(1)] public int gap = 2;

        public float Fill
        {
            get => fill;
            set
            {
                value = Mathf.Clamp01(value);
                if (fill == value)
                    return;
                fill = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            AddBlock(mesh, rect, backgroundColor);
            float scale = canvas != null ? canvas.scaleFactor : 1;
            float pixel = 1 / scale;
            int width = Mathf.Max(1, Mathf.RoundToInt(blockWidth * scale));
            int spacing = Mathf.Max(1, Mathf.RoundToInt(gap * scale));
            int height = Mathf.Max(1, Mathf.Min(Mathf.RoundToInt(blockHeight * scale),
                Mathf.FloorToInt((rect.height * scale - spacing * 2) / 2)));
            int columns = Mathf.FloorToInt((rect.width * scale - spacing) / (width + spacing));
            int filled = Mathf.CeilToInt(columns * fill);
            float bottom = rect.center.y - (height + spacing * .5f) * pixel;
            bottom = Mathf.Round(bottom * scale) * pixel;
            for (int column = 0; column < filled; column++)
                for (int row = 0; row < 2; row++)
                    AddBlock(mesh, new Rect(rect.xMin + (spacing + column * (width + spacing)) * pixel,
                        bottom + row * (height + spacing) * pixel, width * pixel, height * pixel), color);
        }

        static void AddBlock(VertexHelper mesh, Rect rect, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.up);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.one);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
