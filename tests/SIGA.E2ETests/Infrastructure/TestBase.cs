using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SIGA.Application.Common;
using Xunit;

namespace SIGA.E2ETests.Infrastructure;

public abstract class TestBase : IClassFixture<SigaTestWebApplicationFactory>
{
    protected readonly SigaTestWebApplicationFactory Factory;
    protected readonly JsonSerializerOptions JsonOptions;

    protected TestBase(SigaTestWebApplicationFactory factory)
    {
        Factory = factory;

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    protected HttpClient CreateAdminClient() => ApiClientHelper.CreateAdminClient(Factory);

    protected HttpClient CreateRoleClient(string role, int userId = 2, string username = "testuser") =>
        ApiClientHelper.CreateRoleClient(Factory, role, userId, username);

    protected HttpClient CreatePermissionsClient(params string[] permissions) =>
        ApiClientHelper.CreatePermissionsClient(Factory, permissions);

    protected HttpClient CreateAnonymousClient() => ApiClientHelper.CreateAnonymousClient(Factory);

    protected HttpClient CreateClientWithCustomToken(string token) =>
        ApiClientHelper.CreateClientWithCustomToken(Factory, token);

    protected async Task<ApiResponse<T>?> DeserializeApiResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
    }

    protected async Task<StringContent> ToJsonContentAsync<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    protected async Task<bool> IsRouteImplementedAsync(HttpClient client, string url, HttpMethod? method = null)
    {
        try
        {
            var req = new HttpRequestMessage(method ?? HttpMethod.Get, url);
            var res = await client.SendAsync(req);
            // If the status is 404 and content is empty or standard ASP.NET not found, the controller might not exist yet.
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                var body = await res.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body) || body.Contains("Not Found") || !body.Contains("success"))
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
