using System;
using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World.Terrain.Boundaries;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    /// <summary>Decodes native boundary parameters directly into terrain boundary objects.</summary>
    public static class TrnBoundaryReader
    {
        public static Boundary Read(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            switch (kind)
            {
                case "BCIR":
                    var center = new Vector2(reader.Float(), reader.Float());
                    float circleRadius = reader.Float();
                    return new CircleBoundary(center, circleRadius, checked((int)reader.U32()), reader.Float());
                case "BREC":
                    var first = new Vector2(reader.Float(), reader.Float());
                    var second = new Vector2(reader.Float(), reader.Float());
                    return new RectangleBoundary(first, second, checked((int)reader.U32()), reader.Float());
                case "BPOL":
                    var polygon = ReadVertices(reader);
                    return new PolygonBoundary(polygon, checked((int)reader.U32()), reader.Float());
                case "BPLN":
                    var polyline = ReadVertices(reader);
                    int featherType = checked((int)reader.U32());
                    float feather = reader.Float();
                    return new PolylineBoundary(polyline, reader.Float(), featherType, feather);
                default:
                    throw new NotSupportedException("Terrain boundary " + kind);
            }
        }

        static Vector2[] ReadVertices(ByteReader reader)
        {
            int count = checked((int)reader.U32());
            if (count < 2 || count > 65536)
                throw new InvalidDataException("Invalid terrain boundary vertex count");
            var vertices = new Vector2[count];
            for (int i = 0; i < count; i++)
                vertices[i] = new Vector2(reader.Float(), reader.Float());
            return vertices;
        }
    }
}
