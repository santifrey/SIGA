using SIGA.E2ETests.Common;
using Xunit;

namespace SIGA.E2ETests.Fixtures;

[Collection("SigaTestCollection")]
public abstract class SigaTestBase
{
    protected readonly SigaWebApplicationFactory Factory;
    protected readonly ApiTestClient Client;

    protected SigaTestBase(SigaWebApplicationFactory factory)
    {
        Factory = factory;
        Client = new ApiTestClient(factory.CreateClient());
    }

    protected ApiTestClient CreateNewClient()
    {
        return new ApiTestClient(Factory.CreateClient());
    }

    protected async Task<ApiTestClient> GetAdminClientAsync()
    {
        var client = CreateNewClient();
        var token = await TestAuthHelper.LoginAndGetTokenAsync(client, "admin", "Admin123!");
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Test infrastructure failure: POST /api/v1/auth/login failed to return a token for admin.");
        }
        client.SetBearerToken(token);
        return client;
    }

    protected ApiTestClient GetClientWithPermissions(string role, params string[] permissions)
    {
        var client = CreateNewClient();
        var token = TestAuthHelper.GenerateToken(
            userId: 99,
            username: $"user_{role.ToLowerInvariant()}",
            role: role,
            permissions: permissions);
        client.SetBearerToken(token);
        return client;
    }

    protected ApiTestClient GetAnonymousClient()
    {
        var client = CreateNewClient();
        client.ClearAuthorization();
        return client;
    }
}
