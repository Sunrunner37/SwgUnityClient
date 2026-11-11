using System.Collections.Generic;
using NUnit.Framework;
using Swg.Client.Core;

namespace Swg.Client.Tests.Editor
{
    public sealed class SwgIdChecks
    {
        [Test]
        public void IdentityPreservesAll64BitsAndSupportsDictionaryLookup()
        {
            var id = new SwgId(ulong.MaxValue);
            var map = new Dictionary<SwgId, string>
            {
                [id] = "object"
            };
            Assert.That(map[new SwgId(ulong.MaxValue)], Is.EqualTo("object"));
            Assert.That((ulong)id, Is.EqualTo(ulong.MaxValue));
            Assert.That(id.ToString(), Is.EqualTo("18446744073709551615"));
            Assert.That(id.Equals((object)ulong.MaxValue), Is.False);
        }

        [Test]
        public void ZeroMeansNoObjectAndIdsSortAsUnsignedValues()
        {
            Assert.That(SwgId.None.IsNone, Is.True);
            Assert.That(new SwgId(1).IsNone, Is.False);
            Assert.That(new SwgId(ulong.MaxValue).CompareTo(new SwgId(1)), Is.GreaterThan(0));
        }
    }
}
