using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnRoadRule : TrnRule
    {
        public TrnRoad Road;
        public static TrnRoadRule Parse(IffNode version) => new TrnRoadRule
        {
            Kind = "AROA",
            Road = TrnRoad.Parse(version)
        };
    }
}
