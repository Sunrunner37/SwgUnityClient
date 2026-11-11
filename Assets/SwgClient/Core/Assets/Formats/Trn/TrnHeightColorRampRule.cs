using Swg.Client.Core.Assets.Formats.Binary;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnHeightColorRampRule : TrnRule
    {
        public int Operation;
        public float LowHeight;
        public float HighHeight;
        public string ImagePath;
        public static TrnHeightColorRampRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var result = new TrnHeightColorRampRule
            {
                Kind = kind
            };
            result.Operation = (int)reader.U32();
            result.LowHeight = reader.Float();
            result.HighHeight = reader.Float();
            result.ImagePath = reader.CString();
            return result;
        }
    }
}
