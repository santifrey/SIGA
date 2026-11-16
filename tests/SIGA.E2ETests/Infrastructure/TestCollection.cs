using Xunit;

namespace SIGA.E2ETests.Infrastructure;

[CollectionDefinition("E2E Test Suite", DisableParallelization = true)]
public class TestCollection : ICollectionFixture<SigaTestWebApplicationFactory>
{
}
