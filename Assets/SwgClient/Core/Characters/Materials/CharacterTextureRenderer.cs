using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.Characters.Materials
{
    /// <summary>Bakes native BTRT facial layers into an immutable diffuse texture before GPU upload.</summary>
    /// <seealso href="https://github.com/SWG-Source/client-tools/blob/master/src/engine/client/library/clientTextureRenderer/src/shared/BlueprintTextureRendererTemplate.cpp"/>
    internal static class CharacterTextureRenderer
    {
        sealed class ShaderState
        {
            public string Path;
            public int Texture = -1;
            public Color Factor = Color.white;
        }
        readonly struct Vertex
        {
            public readonly Vector2 Position;
            public readonly Vector2 Uv;
            public Vertex(ByteReader reader)
            {
                Position = new Vector2(reader.Float(), reader.Float());
                reader.Float(); // Parallel-projection depth.
                Uv = new Vector2(reader.Float(), reader.Float());
            }
        }

        public static bool TryBake(TreAssetLoader loader, IffNode mesh, int shaderIndex, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, out TextureData texture)
        {
            texture = null;
            var bindings = IffNode.Form(mesh.Children, "TRTS");
            if (bindings == null)
                return false;
            foreach (var node in bindings.Children.Where(node => node.Tag == "TRT "))
            {
                var reader = new ByteReader(node.Data);
                string path = reader.CString();
                int count = checked((int)reader.U32());
                bool applies = false;
                for (int i = 0; i < count; i++)
                {
                    int slot = checked((int)reader.U32());
                    string tag = ReadTag(reader);
                    applies |= slot == shaderIndex && tag == "MAIN";
                }
                if (!applies)
                    continue;
                try
                {
                    texture = Bake(loader, IffNode.Parse(loader.Read(path)), own, owner, textures);
                    return true;
                }
                catch (InvalidDataException error)
                {
                    Debug.LogWarning("Texture renderer " + path + ": " + error.Message + "; using the base texture.");
                    return false;
                }
            }
            return false;
        }

        static TextureData Bake(TreAssetLoader loader, List<IffNode> nodes, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures)
        {
            var root = IffNode.Form(nodes, "BTRT") ?? throw new InvalidDataException("Expected BTRT recipe");
            var version = root.Children.Single(node => node.Tag == "FORM");
            if (version.Kind != "0001")
                throw new InvalidDataException("Unsupported BTRT version " + version.Kind);
            var destination = new ByteReader(version.Chunk("DEST"));
            var result = new TextureData(checked((int)destination.U32()), checked((int)destination.U32()));
            var shaders = IffNode.Form(version.Children, "SHTM").Children.Where(node => node.Tag == "NAME")
                .Select(node => new ShaderState { Path = TreAssetLoader.Normalize(new ByteReader(node.Data).CString()) }).ToArray();
            var names = new ByteReader(IffNode.Form(version.Children, "TXTS").Chunk("INFO"));
            var paths = new string[checked((int)names.U32())];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = TreAssetLoader.Normalize(names.CString());
            var buffers = IffNode.Form(version.Children, "VBS ").Children.Where(node => node.Kind == "VTXA").Select(node =>
            {
                var data = IffNode.Form(node.Children, "0003") ?? throw new InvalidDataException("Unsupported texture-renderer vertex layout");
                var info = new ByteReader(data.Chunk("INFO"));
                uint flags = info.U32();
                var vertices = new Vertex[checked((int)info.U32())];
                var bytes = data.Chunk("DATA");
                if (flags != 0x1101 || bytes.Length != vertices.Length * 20)
                    throw new InvalidDataException("Expected position and one UV set in texture-renderer vertices");
                var reader = new ByteReader(bytes);
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vertex(reader);
                return vertices;
            }).ToArray();

            int Value(string variable, bool isPrivate = true)
            {
                string scoped = variable.StartsWith("/", StringComparison.Ordinal) ? variable : (isPrivate ? "/private/" : "/shared_owner/") + variable;
                return (isPrivate ? own : owner).TryGetValue(scoped, out short value) ? value : 0;
            }
            foreach (var command in IffNode.Form(version.Children, "PCMS").Children.Where(node => node.Kind == "PCMD"))
            {
                var condition = command.Children.FirstOrDefault(node => node.Kind == "COND");
                bool active = condition == null || Evaluate(condition.Children.Single());
                var operations = command.Children.FirstOrDefault(node => node.Kind == (active ? "TOPS" : "FOPS"));
                if (operations == null)
                    continue;
                foreach (var operation in operations.Children.Where(node => node.Tag != "INFO"))
                {
                    var reader = new ByteReader(operation.Data);
                    if (operation.Tag == "NOP ")
                        continue;
                    if (operation.Tag == "STFP")
                    {
                        var shader = shaders[reader.U16()];
                        string tag = ReadTag(reader);
                        string palette = reader.CString();
                        string variable = reader.CString();
                        bool isPrivate = reader.Byte() != 0;
                        if (tag == "MAIN")
                        {
                            var color = CharacterMaterialLoader.PaletteColor(loader, palette, Value(variable, isPrivate));
                            shader.Factor = new Color32(color[0], color[1], color[2], 255);
                        }
                        continue;
                    }
                    var target = shaders[checked((int)reader.U32())];
                    string slot = ReadTag(reader);
                    if (slot != "MAIN")
                        throw new InvalidDataException("Unsupported texture-renderer slot " + slot);
                    switch (operation.Tag)
                    {
                        case "SSTC":
                            target.Texture = checked((int)reader.U32());
                            break;
                        case "SST1":
                        {
                            int start = checked((int)reader.U32());
                            string variable = reader.CString();
                            int count = checked((int)reader.U32());
                            target.Texture = start + Mathf.Clamp(Value(variable), 0, count - 1);
                            break;
                        }
                        case "SST2":
                        {
                            int start = checked((int)reader.U32());
                            string first = reader.CString();
                            int firstCount = checked((int)reader.U32());
                            string second = reader.CString();
                            int secondCount = checked((int)reader.U32());
                            target.Texture = start + Mathf.Clamp(Value(first), 0, firstCount - 1) * secondCount + Mathf.Clamp(Value(second), 0, secondCount - 1);
                            break;
                        }
                        case "STFA":
                        {
                            byte red = reader.Byte();
                            byte green = reader.Byte();
                            byte blue = reader.Byte();
                            target.Factor = new Color32(red, green, blue, (byte)Mathf.Clamp(Value(reader.CString()), 0, 255));
                            break;
                        }
                        default:
                            throw new InvalidDataException("Unsupported texture-renderer operation " + operation.Tag);
                    }
                }
            }
            foreach (var command in IffNode.Form(version.Children, "RCMS").Children.Where(node => node.Tag != "INFO"))
            {
                if (command.Kind != "SRSC")
                    throw new InvalidDataException("Unsupported texture-renderer command " + (command.Kind ?? command.Tag));
                var shader = shaders[checked((int)new ByteReader(command.Chunk("INFO")).U32())];
                if (shader.Texture < 0)
                    continue;
                string path = paths[shader.Texture];
                if (!textures.TryGetValue(path, out var source))
                    textures.Add(path, source = TextureData.Load(loader, path));
                bool copy = shader.Path.Contains("texren_copy_");
                if (!copy && !shader.Path.Contains("texren_alphablend_"))
                    throw new InvalidDataException("Unsupported texture-renderer shader " + shader.Path);
                foreach (var draw in command.Children.Where(node => node.Tag != "INFO"))
                {
                    if (draw.Tag != "RTFC")
                        throw new InvalidDataException("Unsupported texture-renderer primitive " + draw.Tag);
                    var vertices = buffers[checked((int)new ByteReader(draw.Data).U32())];
                    for (int i = 1; i + 1 < vertices.Length; i++)
                        DrawTriangle(result, source, shader.Factor, copy, vertices[0], vertices[i], vertices[i + 1]);
                }
            }
            return result;
        }

        static bool Evaluate(IffNode condition)
        {
            switch (condition.Kind ?? condition.Tag)
            {
                case "TRUE": case "CVTP": return true;
                case "FALS": return false;
                case "OR  ": return condition.Children.Any(node => node.Tag != "INFO" && Evaluate(node));
                case "AND ": return condition.Children.All(node => node.Tag == "INFO" || Evaluate(node));
                default: throw new InvalidDataException("Unsupported texture-renderer condition " + (condition.Kind ?? condition.Tag));
            }
        }

        static string ReadTag(ByteReader reader)
        {
            var bytes = reader.Take(4);
            Array.Reverse(bytes);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        static float Edge(Vector2 a, Vector2 b, Vector2 point) => (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        static bool TopLeft(Vector2 a, Vector2 b) => b.y < a.y || (b.y == a.y && b.x > a.x);

        static void DrawTriangle(TextureData target, TextureData source, Color factor, bool copy, Vertex a, Vertex b, Vertex c)
        {
            var pa = new Vector2(a.Position.x * target.Width, a.Position.y * target.Height);
            var pb = new Vector2(b.Position.x * target.Width, b.Position.y * target.Height);
            var pc = new Vector2(c.Position.x * target.Width, c.Position.y * target.Height);
            float area = Edge(pa, pb, pc);
            if (Mathf.Abs(area) < .00001f)
                return;
            if (area < 0)
            {
                DrawTriangle(target, source, factor, copy, a, c, b);
                return;
            }
            int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))));
            int right = Mathf.Min(target.Width - 1, Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))));
            int top = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))));
            int bottom = Mathf.Min(target.Height - 1, Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))));
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    var point = new Vector2(x + .5f, y + .5f);
                    float wa = Edge(pb, pc, point);
                    float wb = Edge(pc, pa, point);
                    float wc = Edge(pa, pb, point);
                    if (wa < 0 || wb < 0 || wc < 0 || (wa == 0 && !TopLeft(pb, pc)) || (wb == 0 && !TopLeft(pc, pa)) || (wc == 0 && !TopLeft(pa, pb)))
                        continue;
                    var uv = (a.Uv * wa + b.Uv * wb + c.Uv * wc) / area;
                    var color = Sample(source, uv) * factor;
                    int offset = (y * target.Width + x) * 4;
                    if (!copy)
                    {
                        float alpha = color.a;
                        color.r = color.r * alpha + target.Pixels[offset] / 255f * (1 - alpha);
                        color.g = color.g * alpha + target.Pixels[offset + 1] / 255f * (1 - alpha);
                        color.b = color.b * alpha + target.Pixels[offset + 2] / 255f * (1 - alpha);
                        color.a = alpha + target.Pixels[offset + 3] / 255f * (1 - alpha);
                    }
                    Color32 pixel = color;
                    target.Pixels[offset] = pixel.r;
                    target.Pixels[offset + 1] = pixel.g;
                    target.Pixels[offset + 2] = pixel.b;
                    target.Pixels[offset + 3] = pixel.a;
                }
        }

        static Color Sample(TextureData source, Vector2 uv)
        {
            float x = Mathf.Clamp(uv.x * source.Width - .5f, 0, source.Width - 1);
            float y = Mathf.Clamp(uv.y * source.Height - .5f, 0, source.Height - 1);
            int left = (int)x;
            int top = (int)y;
            int right = Mathf.Min(left + 1, source.Width - 1);
            int bottom = Mathf.Min(top + 1, source.Height - 1);
            Color Pixel(int px, int py)
            {
                int at = (py * source.Width + px) * 4;
                return new Color32(source.Pixels[at], source.Pixels[at + 1], source.Pixels[at + 2], source.Pixels[at + 3]);
            }
            return Color.Lerp(Color.Lerp(Pixel(left, top), Pixel(right, top), x - left), Color.Lerp(Pixel(left, bottom), Pixel(right, bottom), x - left), y - top);
        }
    }
}

