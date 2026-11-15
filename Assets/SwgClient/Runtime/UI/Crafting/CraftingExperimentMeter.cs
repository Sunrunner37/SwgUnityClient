using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Crafting
{
    /// <summary>Native-style points stack or animated risk grid, drawn as one UI mesh without per-block objects.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CraftingExperimentMeter : MaskableGraphic
    {
        public bool animateRisk;
        public Texture2D blockTexture;
        public override Texture mainTexture => blockTexture != null ? blockTexture : base.mainTexture;
        public Color inactiveColor = new Color(0, .15f, .18f, .4f);
        int remaining;
        int total;
        float risk;
        int frame = -1;
        public void ShowPoints(int available, int maximum)
        {
            if (remaining == available && total == maximum) return;
            remaining = available;
            total = maximum;
            SetVerticesDirty();
        }
        public void ShowRisk(float percent)
        {
            if (risk == percent) return;
            risk = percent;
            SetVerticesDirty();
        }
        void Update()
        {
            if (!animateRisk || risk <= 0) return;
            int next = Mathf.FloorToInt(Time.unscaledTime / .15f);
            if (next == frame) return;
            frame = next;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            int rows = animateRisk ? 12 : total;
            if (rows <= 0) return;
            int columns = animateRisk ? 7 : 1;
            float gap = 2 / (canvas != null ? canvas.scaleFactor : 1);
            float availableHeight = (rect.height - gap * (rows - 1)) / rows;
            float height = animateRisk ? Mathf.Min(7, availableHeight) : availableHeight;
            float width = (rect.width - gap * (columns - 1)) / columns;
            for (int row = 0; row < rows; row++)
            {
                float opacity = animateRisk ? (risk <= 0 ? 0 : Mathf.Max(.08f, 1 - Mathf.Repeat(row + frame, rows) / 5)) : 1;
                var tint = animateRisk || row < remaining ? color : inactiveColor;
                tint.a *= opacity;
                for (int column = 0; column < columns; column++)
                {
                    float x = rect.xMin + column * (width + gap);
                    float y = rect.yMin + row * (height + gap);
                    int first = mesh.currentVertCount;
                    mesh.AddVert(new Vector3(x, y), tint, Vector2.zero);
                    mesh.AddVert(new Vector3(x, y + height), tint, Vector2.up);
                    mesh.AddVert(new Vector3(x + width, y + height), tint, Vector2.one);
                    mesh.AddVert(new Vector3(x + width, y), tint, Vector2.right);
                    mesh.AddTriangle(first, first + 1, first + 2);
                    mesh.AddTriangle(first, first + 2, first + 3);
                }
            }
        }
    }
}
