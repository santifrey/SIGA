using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SIGA.E2ETests.Common;

public class ApiTestClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpClient HttpClient => _client;

    public ApiTestClient(HttpClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public void SetBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public void ClearAuthorization()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _client.GetAsync(url);
    }

    public async Task<(HttpResponseMessage Response, ApiResponseDto<T>? Body)> GetJsonAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        ApiResponseDto<T>? body = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                body = JsonSerializer.Deserialize<ApiResponseDto<T>>(content, _jsonOptions);
            }
            catch
            {
                // Non-JSON or different format
            }
        }
        return (response, body);
    }

    public async Task<HttpResponseMessage> PostAsync<TRequest>(string url, TRequest payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(url, content);
    }

    public async Task<(HttpResponseMessage Response, ApiResponseDto<TResponse>? Body)> PostJsonAsync<TRequest, TResponse>(string url, TRequest payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(url, content);
        var resContent = await response.Content.ReadAsStringAsync();
        ApiResponseDto<TResponse>? body = null;
        if (!string.IsNullOrWhiteSpace(resContent))
        {
            try
            {
                body = JsonSerializer.Deserialize<ApiResponseDto<TResponse>>(resContent, _jsonOptions);
            }
            catch
            {
                // Non-JSON or different format
            }
        }
        return (response, body);
    }

    public async Task<HttpResponseMessage> PutAsync<TRequest>(string url, TRequest payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PutAsync(url, content);
    }

    public async Task<(HttpResponseMessage Response, ApiResponseDto<TResponse>? Body)> PutJsonAsync<TRequest, TResponse>(string url, TRequest payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync(url, content);
        var resContent = await response.Content.ReadAsStringAsync();
        ApiResponseDto<TResponse>? body = null;
        if (!string.IsNullOrWhiteSpace(resContent))
        {
            try
            {
                body = JsonSerializer.Deserialize<ApiResponseDto<TResponse>>(resContent, _jsonOptions);
            }
            catch
            {
                // Non-JSON or different format
            }
        }
        return (response, body);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await _client.DeleteAsync(url);
    }

    public async Task<(HttpResponseMessage Response, ApiResponseDto<TResponse>? Body)> DeleteJsonAsync<TResponse>(string url)
    {
        var response = await _client.DeleteAsync(url);
        var resContent = await response.Content.ReadAsStringAsync();
        ApiResponseDto<TResponse>? body = null;
        if (!string.IsNullOrWhiteSpace(resContent))
        {
            try
            {
                body = JsonSerializer.Deserialize<ApiResponseDto<TResponse>>(resContent, _jsonOptions);
            }
            catch
            {
                // Non-JSON or different format
            }
        }
        return (response, body);
    }
}
