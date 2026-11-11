using System;
using System.Linq;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnRoad
    {
        public Vector2[] Points;
        public Vector3[][] Segments;
        public float[] FixedHeights = Array.Empty<float>();
        public float Width;
        public int ShaderFamily;
        public int HeightFeatherType;
        public float HeightFeather;
        public int ShaderFeatherType;
        public float ShaderFeather;
        public bool FixedHeight;
        public static TrnRoad Parse(IffNode version) => new TrnRoad(version);
        TrnRoad(IffNode version)
        {
            if (version.Kind != "0005" && version.Kind != "0006")
                throw new NotSupportedException("Road affector version " + version.Kind);
            var data = version.Children.First(node => node.Kind == "DATA");
            var reader = new ByteReader(data.Chunk("DATA"));
            Points = new Vector2[reader.Count()];
            for (int i = 0; i < Points.Length; i++)
                Points[i] = new Vector2(reader.Float(), reader.Float());
            if (version.Kind == "0006")
            {
                FixedHeights = new float[reader.Count()];
                for (int i = 0; i < FixedHeights.Length; i++)
                    FixedHeights[i] = reader.Float();
            }

            Width = reader.Float();
            ShaderFamily = (int)reader.U32();
            HeightFeatherType = (int)reader.U32();
            HeightFeather = reader.Float();
            ShaderFeatherType = (int)reader.U32();
            ShaderFeather = reader.Float();
            FixedHeight = version.Kind == "0006" && reader.U32() != 0;
            var heightData = data.Children.FirstOrDefault(node => node.Kind == "HDTA" || node.Kind == "ROAD");
            var samples = heightData == null ? Array.Empty<IffNode>() : heightData.Children.SelectMany(node => node.Children).Where(node => node.Tag == "SGMT").ToArray();
            Segments = samples.Select(node =>
            {
                var sample = new ByteReader(node.Data);
                var values = new Vector3[node.Data.Length / 12];
                for (int i = 0; i < values.Length; i++)
                    values[i] = new Vector3(sample.Float(), sample.Float(), sample.Float());
                return values;
            }).ToArray();
        }
    }
}
