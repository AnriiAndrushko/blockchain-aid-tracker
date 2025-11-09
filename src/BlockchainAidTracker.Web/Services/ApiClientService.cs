using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.Extensions.Options;

namespace BlockchainAidTracker.Web.Services;

public class ApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILocalStorageService _localStorage;

    public ApiClientService(HttpClient httpClient, IOptions<ApiSettings> apiSettings, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _httpClient.BaseAddress = new Uri(apiSettings.Value.BaseUrl);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task AddAuthorizationHeaderAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop not available during server-side prerendering
            // This is expected - continue without token
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint)
    {
        try
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GET request failed: {ex.Message}");
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"POST request failed: {ex.Message}");
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PUT request failed: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DELETE request failed: {ex.Message}");
            return false;
        }
    }

    public async Task<HttpResponseMessage> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        await AddAuthorizationHeaderAsync();
        return await _httpClient.PostAsJsonAsync(endpoint, data);
    }

    public async Task<HttpResponseMessage> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        await AddAuthorizationHeaderAsync();
        return await _httpClient.PutAsJsonAsync(endpoint, data);
    }
}
