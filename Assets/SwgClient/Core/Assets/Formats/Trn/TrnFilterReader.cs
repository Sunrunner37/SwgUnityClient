using System;
using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Core.World.Terrain.Filters;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    /// <summary>Decodes filters directly into terrain objects, resolving their shared fractal dependencies.</summary>
    public sealed class TrnFilterReader
    {
        readonly IReadOnlyDictionary<int, NativeTerrainFractal> fractals;
        public TrnFilterReader(IReadOnlyDictionary<int, NativeTerrainFractal> fractals)
        {
            this.fractals = fractals;
        }

        public TerrainFilter Read(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            switch (kind)
            {
                case "FHGT":
                    return new TerrainHeightFilter(reader.Float(), reader.Float(), checked((int)reader.U32()), reader.Float());
                case "FFRA":
                    int id = checked((int)reader.U32());
                    return new TerrainFractalFilter(id, fractals[id], checked((int)reader.U32()), reader.Float(), reader.Float(), reader.Float(), reader.Float());
                case "FSLP":
                    return new TerrainSlopeFilter(reader.Float(), reader.Float(), checked((int)reader.U32()), reader.Float());
                case "FSHD":
                    return new TerrainShaderFilter(checked((int)reader.U32()));
                default:
                    throw new NotSupportedException("Terrain filter " + kind);
            }
        }
    }
}
