using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, (await _localStorage.GetItemAsync<string>("username")) ?? string.Empty)
                }, "jwt")));
    }

    public async Task MarkUserAsAuthenticated(string username)
    {
        await _localStorage.SetItemAsync("username", username);
        
        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, username)
            }, "jwt"));

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("username");
        await _localStorage.RemoveItemAsync("authToken");

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    public void UpdateUserInfo(User userInfo)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userInfo.Username ?? ""),
            new Claim("avatar_url", userInfo.AvatarUrl ?? ""),
            new Claim("user_id", userInfo.Id?.ToString() ?? ""),
            // 可以添加其他需要的用户信息
        }, "jwt");

        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }
} 