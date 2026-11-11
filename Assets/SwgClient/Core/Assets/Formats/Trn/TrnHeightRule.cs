using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnHeightRule : TrnRule
    {
        public int FractalId;
        public int Operation;
        public float Value;
        public static TrnHeightRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var height = new TrnHeightRule
            {
                Kind = kind
            };
            if (kind == "AHFR")
                height.FractalId = (int)reader.U32();
            height.Operation = (int)reader.U32();
            height.Value = reader.Float();
            return height;
        }
    }
}
