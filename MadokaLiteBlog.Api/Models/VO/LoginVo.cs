namespace MadokaLiteBlog.Api.Models.VO;

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; } = string.Empty;
    public required string AvatarUrl { get; set; } = string.Empty;
    public required string Motto { get; set; } = string.Empty;
}

