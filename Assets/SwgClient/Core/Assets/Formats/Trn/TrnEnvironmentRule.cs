using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnEnvironmentRule : TrnRule
    {
        public int FamilyId;
        public bool UseOverride;
        public float Threshold;
        public static TrnEnvironmentRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var environment = new TrnEnvironmentRule
            {
                Kind = kind
            };
            environment.FamilyId = (int)reader.U32();
            environment.UseOverride = reader.U32() != 0;
            environment.Threshold = reader.Float();
            return environment;
        }
    }
}
