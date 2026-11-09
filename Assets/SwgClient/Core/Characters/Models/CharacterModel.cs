using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Models
{
    public static class CharacterModel
    {
        public static List<MeshPart> Load(TreAssetLoader loader, string objectPath)
        {
            var objectBytes = loader.Read(objectPath);
            var satPath = IffNode.References(objectBytes, "appearance/", ".sat").FirstOrDefault() ?? throw new InvalidDataException("No skeletal appearance in player template");
            var sat = loader.Read(satPath);
            var lmgs = IffNode.References(sat, "appearance/mesh/", ".lmg");
            if (lmgs.Count == 0)
                throw new InvalidDataException("No meshes in player appearance");
            var materials = new Dictionary<string, TextureData>();
            var all = new List<MeshPart>();
            foreach (string lmg in lmgs)
            {
                string mgn = IffNode.References(loader.Read(lmg), "appearance/mesh/", ".mgn").FirstOrDefault() ?? throw new InvalidDataException("No mesh LOD");
                all.AddRange(LoadMesh(loader, mgn, materials));
            }

            return all;
        }

        static List<MeshPart> LoadMesh(TreAssetLoader loader, string path, Dictionary<string, TextureData> materials)
        {
            var skmg = IffNode.Form(IffNode.Parse(loader.Read(path)), "SKMG") ?? throw new InvalidDataException("Not a skeletal mesh");
            var version = skmg.Children.FirstOrDefault(n => n.Kind == "0004") ?? throw new InvalidDataException("Unsupported skeletal mesh version");
            var positions = version.Chunk("POSN");
            var normals = version.Chunk("NORM");
            var result = new List<MeshPart>();
            foreach (var psdt in version.Children.Where(n => n.Kind == "PSDT"))
            {
                string shader = new ByteReader(psdt.Chunk("NAME")).CString();
                var shaderBytes = loader.Read(shader);
                if (!materials.TryGetValue(shader, out var texture))
                {
                    var textures = IffNode.References(shaderBytes, "texture/", ".dds");
                    string diffuse = textures.FirstOrDefault(p => !p.EndsWith("_cn.dds") && !p.EndsWith("_n.dds") && !p.EndsWith("_spec.dds") && !p.EndsWith("_sm.dds")) ?? textures.FirstOrDefault() ?? throw new InvalidDataException("Shader has no diffuse texture");
                    texture = TextureData.Load(loader, diffuse);
                    materials.Add(shader, texture);
                }

                byte[] tint = null;
                var pal = IffNode.References(shaderBytes, "palette/", ".pal").FirstOrDefault(p => p.Contains("pc_skin_"));
                if (pal != null)
                {
                    var bytes = loader.Read(pal);
                    if (bytes.Length < 28 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "PAL ")
                        throw new InvalidDataException("Invalid skin palette");
                    tint = new[]
                    {
                        bytes[24],
                        bytes[25],
                        bytes[26]
                    };
                }

                var indices = new ByteReader(psdt.Chunk("PIDX"));
                int count = checked((int)indices.U32());
                if (count > 100000 || indices.Remaining != count * 4)
                    throw new InvalidDataException("Invalid mesh vertex count");
                var nidx = new ByteReader(psdt.Chunk("NIDX"));
                var uvBytes = IffNode.Form(psdt.Children, "TCSF")?.Chunk("TCSD") ?? throw new InvalidDataException("Missing texture coordinates");
                var uv = new ByteReader(uvBytes);
                var vertices = new Float3[count];
                var ns = new Float3[count];
                var uvs = new Float2[count];
                for (int i = 0; i < count; i++)
                {
                    int p = checked((int)indices.U32() * 12);
                    int n = checked((int)nidx.U32() * 12);
                    var rp = new ByteReader(positions)
                    {
                        Position = p
                    };
                    var rn = new ByteReader(normals)
                    {
                        Position = n
                    };
                    vertices[i] = new Float3(rp.Float(), rp.Float(), rp.Float());
                    ns[i] = new Float3(rn.Float(), rn.Float(), rn.Float());
                    uvs[i] = new Float2(uv.Float(), uv.Float());
                }

                var triangles = new List<int>();
                var prim = IffNode.Form(psdt.Children, "PRIM") ?? throw new InvalidDataException("Missing mesh primitives");
                foreach (var n in prim.Children)
                {
                    if (n.Tag != "OITL" && n.Tag != "ITL ")
                        continue;
                    var r = new ByteReader(n.Data);
                    int cnt = checked((int)r.U32());
                    int stride = n.Tag == "OITL" ? 14 : 12;
                    if (cnt > 200000 || r.Remaining != cnt * stride)
                        throw new InvalidDataException("Invalid mesh triangle count");
                    for (int i = 0; i < cnt; i++)
                    {
                        if (stride == 14)
                            r.U16();
                        for (int v = 0; v < 3; v++)
                        {
                            int index = checked((int)r.U32());
                            if (index >= count)
                                throw new InvalidDataException("Invalid mesh vertex index");
                            triangles.Add(index);
                        }
                    }
                }

                result.Add(new MeshPart { Vertices = vertices, Normals = ns, Uvs = uvs, Triangles = triangles.ToArray(), Diffuse = texture, Tint = tint });
            }

            return result;
        }
    }
}
