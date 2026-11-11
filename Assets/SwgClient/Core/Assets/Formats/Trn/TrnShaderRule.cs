using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnShaderRule : TrnRule
    {
        public int SourceFamilyId = -1;
        public int FamilyId;
        public bool UseOverride;
        public float Threshold;
        public static TrnShaderRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var shader = new TrnShaderRule
            {
                Kind = kind
            };
            shader.SourceFamilyId = kind == "ASRP" ? (int)reader.U32() : -1;
            shader.FamilyId = (int)reader.U32();
            shader.UseOverride = reader.Remaining >= 8 && reader.U32() != 0;
            shader.Threshold = reader.Remaining >= 4 ? reader.Float() : 0;
            return shader;
        }
    }
}
