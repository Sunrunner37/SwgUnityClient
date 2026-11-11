using Swg.Client.Core.Assets.Formats.Binary;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnConstantColorRule : TrnRule
    {
        public int Operation;
        public Color32 Color;
        public static TrnConstantColorRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var result = new TrnConstantColorRule
            {
                Kind = kind
            };
            result.Operation = (int)reader.U32();
            result.Color = new Color32(reader.Byte(), reader.Byte(), reader.Byte(), 255);
            return result;
        }
    }
}
