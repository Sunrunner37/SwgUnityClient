using NUnit.Framework;

namespace Swg.Client.Tests.Editor
{
    [SetUpFixture]
    public sealed class ReplicationTestCleanup
    {
        [OneTimeTearDown]
        public void DestroyTestComponents() => GameServicesForTests.DestroyHelpers();
    }
}
