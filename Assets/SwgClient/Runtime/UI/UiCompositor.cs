using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Runtime.UI
{
    public sealed class UiCompositor
    {
        sealed class Sources
        {
            public readonly Dictionary<string, TextureData> Atlases = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, UiNode> Styles = new Dictionary<string, UiNode>(StringComparer.OrdinalIgnoreCase);
        }

        static readonly ConditionalWeakTable<TreAssetLoader, Sources> sharedSources = new ConditionalWeakTable<TreAssetLoader, Sources>();
        readonly TextureData atlas;
        readonly UiNode styles;
        public UiCompositor(TreAssetLoader loader, string atlasPath, string stylesPath)
        {
            // Decoded sources are immutable during composition and belong to this mounted asset set.
            var sources = sharedSources.GetValue(loader, _ => new Sources());
            atlasPath = TreAssetLoader.Normalize(atlasPath);
            stylesPath = TreAssetLoader.Normalize(stylesPath);
            lock (sources)
            {
                if (!sources.Atlases.TryGetValue(atlasPath, out atlas))
                {
                    atlas = TextureData.Load(loader, atlasPath);
                    sources.Atlases.Add(atlasPath, atlas);
                }

                if (!sources.Styles.TryGetValue(stylesPath, out styles))
                {
                    styles = UiNode.Parse(loader.Text(stylesPath));
                    sources.Styles.Add(stylesPath, styles);
                }
            }
        }

        public void Nine(TextureData dst, string name, int x, int y, int w, int h, int[] tint = null, float opacity = 1, string state = "default")
        {
            var style = styles.FindPath(name) ?? throw new InvalidDataException("Missing UI style " + name);
            var node = string.IsNullOrEmpty(state) ? style : style.Children.FirstOrDefault(c => c.Get("Name") == state) ?? throw new InvalidDataException("Missing UI state " + name + "/" + state);
            var p = node.Children.Where(c => c.Get("SourceRect") != "").ToDictionary(c => c.Get("Name"), c => c.Get("SourceRect").Split(',').Select(int.Parse).ToArray());
            int Width(string k) => p.TryGetValue(k, out var v) ? Math.Abs(v[2] - v[0]) : 0;
            int Height(string k) => p.TryGetValue(k, out var v) ? Math.Abs(v[3] - v[1]) : 0;
            int l = Math.Max(Width("nw"), Width("w"));
            int r = Math.Max(Width("ne"), Width("e"));
            int t = Height("nw");
            int b = Height("sw");
            foreach (var pair in p)
            {
                int[] rect;
                switch (pair.Key)
                {
                    case "nw":
                        rect = new[]
                        {
                            x,
                            y,
                            l,
                            t
                        };
                        break;
                    case "ne":
                        rect = new[]
                        {
                            x + w - r,
                            y,
                            r,
                            t
                        };
                        break;
                    case "n":
                        rect = new[]
                        {
                            x + l,
                            y,
                            w - l - r,
                            t
                        };
                        break;
                    case "sw":
                        rect = new[]
                        {
                            x,
                            y + h - b,
                            l,
                            b
                        };
                        break;
                    case "se":
                        rect = new[]
                        {
                            x + w - r,
                            y + h - b,
                            r,
                            b
                        };
                        break;
                    case "s":
                        rect = new[]
                        {
                            x + l,
                            y + h - b,
                            w - l - r,
                            b
                        };
                        break;
                    case "w":
                        rect = new[]
                        {
                            x,
                            y + t,
                            l,
                            h - t - b
                        };
                        break;
                    case "e":
                        rect = new[]
                        {
                            x + w - r,
                            y + t,
                            r,
                            h - t - b
                        };
                        break;
                    case "c":
                        rect = new[]
                        {
                            x + l,
                            y + t,
                            w - l - r,
                            h - t - b
                        };
                        break;
                    default:
                        continue;
                }

                Copy(dst, pair.Value, rect, tint ?? new[] { 255, 255, 255 }, opacity);
            }
        }

        public void Copy(TextureData dst, int[] sr, int[] dr, int[] tint, float opacity)
        {
            int sx = sr[0];
            int sy = sr[1];
            int ex = sr[2];
            int ey = sr[3];
            int dx = dr[0];
            int dy = dr[1];
            int dw = dr[2];
            int dh = dr[3];
            if (dw <= 0 || dh <= 0)
                return;
            for (int y = 0; y < dh; y++)
                for (int x = 0; x < dw; x++)
                {
                    if (dx + x < 0 || dy + y < 0 || dx + x >= dst.Width || dy + y >= dst.Height)
                        continue;
                    // Filter within this atlas region, using premultiplied alpha to avoid dark fringes.
                    float px = Math.Clamp((float)(sx + (x + .5) * (ex - sx) / dw - .5), Math.Min(sx, ex), Math.Max(sx, ex) - 1);
                    float py = Math.Clamp((float)(sy + (y + .5) * (ey - sy) / dh - .5), Math.Min(sy, ey), Math.Max(sy, ey) - 1);
                    int x0 = (int)Math.Floor(px);
                    int y0 = (int)Math.Floor(py);
                    int x1 = Math.Min(x0 + 1, Math.Max(sx, ex) - 1);
                    int y1 = Math.Min(y0 + 1, Math.Max(sy, ey) - 1);
                    float fx = px - x0;
                    float fy = py - y0;
                    int p00 = (y0 * atlas.Width + x0) * 4;
                    int p10 = (y0 * atlas.Width + x1) * 4;
                    int p01 = (y1 * atlas.Width + x0) * 4;
                    int p11 = (y1 * atlas.Width + x1) * 4;
                    float a00 = atlas.Pixels[p00 + 3] / 255f * (1 - fx) * (1 - fy);
                    float a10 = atlas.Pixels[p10 + 3] / 255f * fx * (1 - fy);
                    float a01 = atlas.Pixels[p01 + 3] / 255f * (1 - fx) * fy;
                    float a11 = atlas.Pixels[p11 + 3] / 255f * fx * fy;
                    float a = (a00 + a10 + a01 + a11) * opacity;
                    int d = ((dy + y) * dst.Width + dx + x) * 4;
                    float oa = dst.Pixels[d + 3] / 255f;
                    float na = a + oa * (1 - a);
                    if (na == 0)
                        continue;
                    for (int c = 0; c < 3; c++)
                    {
                        float source = atlas.Pixels[p00 + c] * a00 + atlas.Pixels[p10 + c] * a10 + atlas.Pixels[p01 + c] * a01 + atlas.Pixels[p11 + c] * a11;
                        dst.Pixels[d + c] = (byte)Math.Round((source * tint[c] / 255f * opacity + dst.Pixels[d + c] * oa * (1 - a)) / na);
                    }

                    dst.Pixels[d + 3] = (byte)Math.Round(na * 255);
                }
        }
    }
}
