using System;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    /// <summary>Decoded parameters for a supported terrain affector. Kind is its native IFF tag.</summary>
    public abstract class TrnRule
    {
        public string Kind;
        public static TrnRule Parse(string kind, IffNode version, byte[] data) => kind switch
        {
            "AROA" => TrnRoadRule.Parse(version),
            "AFDN" or "AFDF" or "AFSC" or "AFSN" => TrnFloraRule.Parse(kind, data),
            "ASCN" or "ASRP" => TrnShaderRule.Parse(kind, data),
            "AENV" => TrnEnvironmentRule.Parse(kind, data),
            "ACRF" => TrnColorRampRule.Parse(kind, data),
            "AHTR" => TrnTerraceRule.Parse(kind, data),
            "AHFR" or "AHCN" => TrnHeightRule.Parse(kind, data),
            _ => throw new NotSupportedException("Terrain rule " + kind)};
    }
}
