using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Idtl
{
    /// <summary>IDTL (indexed triangle list) is SWG's compact shared geometry format, used for radar silhouettes, portals and collision.</summary>
    /// <seealso href="https://github.com/swgemu/Core3/blob/unstable/MMOCoreORB/src/templates/collision/CollisionMeshVolume.cpp"/>
    public sealed class IdtlFile
    {
        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }
        public IdtlFile(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public static IdtlFile Parse(IffNode root)
        {
            if (root.Kind != "IDTL") throw new InvalidDataException("Expected IDTL geometry.");
            var version = root.Children.Single(node => node.Tag == "FORM");
            if (version.Kind != "0000") throw new InvalidDataException("Unsupported IDTL version " + version.Kind);
            var vertices = new ByteReader(version.Chunk("VERT"));
            var indices = new ByteReader(version.Chunk("INDX"));
            if (vertices.Remaining % 12 != 0 || indices.Remaining % 12 != 0)
                throw new InvalidDataException("Incomplete IDTL triangle geometry.");
            var points = new Vector3[vertices.Remaining / 12];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector3(vertices.Float(), vertices.Float(), vertices.Float());
            var triangles = new int[indices.Remaining / 4];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = checked((int)indices.U32());
                if (triangles[i] >= points.Length) throw new InvalidDataException("IDTL vertex index is outside its geometry.");
            }
            return new IdtlFile(points, triangles);
        }

        public IdtlFile Transformed(Matrix4x4 transform) => new IdtlFile(Vertices.Select(transform.MultiplyPoint3x4).ToArray(), Triangles);
    }
}
