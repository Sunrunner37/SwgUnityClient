using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnFloraRule : TrnRule
    {
        public int FamilyId;
        public int Operation;
        public bool RemoveAll;
        public bool UseOverride;
        public float Density = 1;
        public static TrnFloraRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var flora = new TrnFloraRule
            {
                Kind = kind
            };
            bool dynamic = kind == "AFDN" || kind == "AFDF";
            flora.FamilyId = (int)reader.U32();
            flora.Operation = reader.Remaining >= 4 ? (int)reader.U32() : dynamic ? 1 : 0;
            flora.RemoveAll = reader.Remaining >= 4 && reader.U32() != 0;
            flora.UseOverride = reader.Remaining >= 4 && reader.U32() != 0;
            flora.Density = reader.Remaining >= 4 ? reader.Float() : 1;
            return flora;
        }
    }
}
