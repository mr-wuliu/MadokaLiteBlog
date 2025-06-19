using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using System.Text.Json.Nodes;
using MadokaLiteBlog.Client.Models;

namespace MadokaLiteBlog.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;
    private readonly NavigationManager _navigationManager;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http, NavigationManager navigationManager)
    {
        _localStorage = localStorage;
        _http = http;
        _navigationManager = navigationManager;
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            // JWT token 由三部分组成，用点号分隔：header.payload.signature
            var parts = token.Split('.');
            if (parts.Length != 3) return true;

            // 解码 payload（第二部分）
            var payload = parts[1];
            var padding = new string('=', (4 - (payload.Length % 4)) % 4);
            var base64 = payload.Replace('-', '+').Replace('_', '/') + padding;
            var jsonBytes = Convert.FromBase64String(base64);
            var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
            
            // 解析 JSON
            var json = JsonNode.Parse(jsonString);
            if (json == null) return true;

            // 获取过期时间（exp 字段是 Unix 时间戳）
            var exp = json["exp"]?.GetValue<long>();
            if (exp == null) return true;

            // 转换为 DateTime 并比较
            var expireDate = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime;
            return expireDate < DateTime.UtcNow;
        }
        catch
        {
            return true; // 如果解析失败，视为过期
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // 检查token是否过期
        if (IsTokenExpired(token))
        {
            await MarkUserAsLoggedOut();
            _navigationManager.NavigateTo("/login", true);
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
        _http.DefaultRequestHeaders.Authorization = null;

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

    public async Task HandleAuthenticationError()
    {
        await MarkUserAsLoggedOut();
        _navigationManager.NavigateTo("/login", true);
    }
} 