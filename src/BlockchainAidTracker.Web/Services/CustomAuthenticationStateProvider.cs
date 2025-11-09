using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlockchainAidTracker.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _jwtHandler;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(_anonymous);
            }

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Validate token is not expired
            var jwtToken = _jwtHandler.ReadJwtToken(token);
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                // Token expired, try to refresh
                var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var refreshed = await RefreshTokenAsync(refreshToken);
                    if (refreshed)
                    {
                        token = await _localStorage.GetItemAsync<string>("accessToken");
                        jwtToken = _jwtHandler.ReadJwtToken(token!);
                    }
                    else
                    {
                        await MarkUserAsLoggedOut();
                        return new AuthenticationState(_anonymous);
                    }
                }
                else
                {
                    await MarkUserAsLoggedOut();
                    return new AuthenticationState(_anonymous);
                }
            }

            var claims = jwtToken.Claims.ToList();
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(string accessToken, string refreshToken, string userId, string username, string role)
    {
        await _localStorage.SetItemAsync("accessToken", accessToken);
        await _localStorage.SetItemAsync("refreshToken", refreshToken);
        await _localStorage.SetItemAsync("userId", userId);
        await _localStorage.SetItemAsync("username", username);
        await _localStorage.SetItemAsync("userRole", role);

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var jwtToken = _jwtHandler.ReadJwtToken(accessToken);
        var claims = jwtToken.Claims.ToList();
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("accessToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("userId");
        await _localStorage.RemoveItemAsync("username");
        await _localStorage.RemoveItemAsync("userRole");

        _httpClient.DefaultRequestHeaders.Authorization = null;

        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }

    public async Task RefreshAuthenticationState()
    {
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshed = await RefreshTokenAsync(refreshToken);
            if (refreshed)
            {
                // Notify all components that authentication state has changed
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
        }
    }

    private async Task<bool> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/authentication/refresh-token", new { refreshToken });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync("accessToken", result.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);

                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private class AuthenticationResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
