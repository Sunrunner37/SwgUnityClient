using Swg.Client.Core.Assets.Formats.Binary;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    public sealed class TrnExcludeRule : TrnRule
    {
        public static TrnExcludeRule Parse(string kind, byte[] data)
        {
            var reader = new ByteReader(data);
            var result = new TrnExcludeRule
            {
                Kind = kind
            };
            return result;
        }
    }
}
