using System.Net.Http.Json;
using MadokaLiteBlog.Client.Models;

namespace MadokaLiteBlog.Client.Services;

public interface IUserService
{
    Task<User?> GetCurrentUserInfo();
    Task UpdateUserInfo(User userInfo);
}

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private User? _currentUser;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<User?> GetCurrentUserInfo()
    {
        if (_currentUser == null)
        {
            var response = await _httpClient.PostAsync("api/user/info", null);
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<User>();
            }
        }
        return _currentUser;
    }

    public Task UpdateUserInfo(User userInfo)
    {
        _currentUser = userInfo;
        return Task.CompletedTask;
    }
} 