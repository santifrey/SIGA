using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SIGA.E2ETests.Fixtures;

[CollectionDefinition("SigaTestCollection")]
public class SigaTestCollection : ICollectionFixture<SigaWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
