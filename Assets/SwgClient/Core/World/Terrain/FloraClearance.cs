using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>A native clearance circle. Object templates clear collidable flora; CNCF client-data instructions clear noncollidable flora.</summary>
    public readonly struct FloraClearance
    {
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly bool Collidable;
        public FloraClearance(Vector3 center, float radius, bool collidable = false)
        {
            Center = center;
            Radius = radius;
            Collidable = collidable;
        }

        public bool Contains(Vector3 point)
        {
            float x = point.x - Center.x;
            float z = point.z - Center.z;
            return x * x + z * z < Radius * Radius;
        }

        /// <summary>Reads CNCF instructions from a general CDF (client-data IFF) file.</summary>
        /// <seealso href="https://github.com/swg-source/client-tools/blob/master/src/engine/client/library/clientGame/src/shared/objectTemplate/ClientDataFile.cpp"/>
        public static FloraClearance[] FromClientData(byte[] bytes)
        {
            var result = new List<FloraClearance>();
            Read(IffNode.Parse(bytes), result);
            return result.ToArray();
        }

        static void Read(IEnumerable<IffNode> nodes, List<FloraClearance> result)
        {
            foreach (var node in nodes)
            {
                if (node.Tag == "CNCF")
                {
                    var reader = new ByteReader(node.Data);
                    result.Add(new FloraClearance(new Vector3(reader.Float(), reader.Float(), reader.Float()), reader.Float()));
                }

                Read(node.Children, result);
            }
        }
    }
}
