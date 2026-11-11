using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnTerraceRule : TrnRule
    {
        public float Value;
        public float Step;
        public static TrnTerraceRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var terrace = new TrnTerraceRule
            {
                Kind = kind
            };
            terrace.Value = reader.Float();
            terrace.Step = reader.Float();
            return terrace;
        }
    }
}
