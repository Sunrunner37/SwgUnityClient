using NUnit.Framework;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Tests.Editor
{
    public sealed class LocalizationChecks
    {
        [Test]
        [SetCulture("fr-FR")]
        public void ProseTokensResolveInOnePassUsingInvariantNumbers()
        {
            var prose = new ProseParameters("Misty %DI", "Trixie", "item", 42, 1.5f);
            var formatter = new ProseFormatter();
            Assert.That(formatter.Format("%TU gives %DI %TO to %TT (%DF). %XX", prose),
                Is.EqualTo("Misty %DI gives 42 item to Trixie (1.5). %XX"));
        }
    }
}
