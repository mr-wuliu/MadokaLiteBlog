namespace MadokaLiteBlog.Api.Models.VO;

public class LoginRequest
{
    public required string Username { get; set; }
    public required string EncryptedPassword { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}