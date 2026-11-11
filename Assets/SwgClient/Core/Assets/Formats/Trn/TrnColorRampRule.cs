using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnColorRampRule : TrnRule
    {
        public int FractalId;
        public int Operation;
        public string ImagePath;
        public static TrnColorRampRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var color = new TrnColorRampRule
            {
                Kind = kind
            };
            color.FractalId = (int)reader.U32();
            color.Operation = (int)reader.U32();
            color.ImagePath = reader.CString();
            return color;
        }
    }
}
