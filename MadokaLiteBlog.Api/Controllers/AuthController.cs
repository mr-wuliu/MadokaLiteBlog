using MadokaLiteBlog.Api.Models.VO;
using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// 用于测试的请求模型
public class TestLoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly RsaHelper _rsaHelper;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IConfiguration configuration, 
        RsaHelper rsaHelper,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _rsaHelper = rsaHelper;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("generate-keys")]
    public async Task<IActionResult> GenerateKeys()
    {
        // 1. 只允许在开发环境使用
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("生产环境尝试生成密钥");
            return NotFound();
        }

        // 2. 只允许本地请求
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (clientIp != "127.0.0.1" && clientIp != "::1")
        {
            _logger.LogWarning("非本地IP尝试生成密钥: {IP}", clientIp);
            return NotFound();
        }

        try
        {
            var (publicKey, _) = await _rsaHelper.GenerateAndSaveKeys();
            _logger.LogInformation("RSA密钥对已更新");
            return Ok(new { publicKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成RSA密钥对时发生错误");
            return StatusCode(500, "生成密钥失败");
        }
    }

    [HttpGet("public-key")]
    public IActionResult GetPublicKey()
    {
        var publicKey = _configuration["Rsa:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
        {
            return NotFound("未找到公钥");
        }
        return Ok(new { publicKey });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            var privateKey = _configuration["Rsa:PrivateKey"];
            if (string.IsNullOrEmpty(privateKey))
            {
                _logger.LogError("未找到RSA私钥");
                return StatusCode(500, "服务器配置错误");
            }

            var decryptedPassword = RsaHelper.Decrypt(request.EncryptedPassword, privateKey);
            
            // TODO: 验证用户名和解密后的密码
            if (!ValidateUser(request.Username, decryptedPassword))
            {
                return Unauthorized("用户名或密码错误");
            }

            var token = GenerateJwtToken(request.Username);
            
            Response.Cookies.Append("X-Access-Token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(15)
            });

            return Ok(new LoginResponse {Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生错误");
            return BadRequest("登录失败");
        }
    }

    private bool ValidateUser(string username, string password)
    {
        // TODO: 这里应该实现真实的用户验证逻辑
        // 例如：查询数据库，验证用户名和密码
        
        // 这里仅作示例，实际使用时应替换为真实的验证逻辑
        return username == "admin" && password == "123456";
    }

    private string GenerateJwtToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("encrypt-login")]
    public IActionResult EncryptLoginRequest([FromBody] TestLoginRequest request)
    {
        // 仅在开发环境可用
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var publicKey = _configuration["Rsa:PublicKey"];
            if (string.IsNullOrEmpty(publicKey))
            {
                return NotFound("未找到公钥，请先生成密钥对");
            }

            // 加密密码
            var encryptedPassword = RsaHelper.Encrypt(request.Password, publicKey);

            // 返回可以直接用于登录接口的请求格式
            return Ok(new
            {
                loginRequest = new LoginRequest
                {
                    Username = request.Username,
                    EncryptedPassword = encryptedPassword
                },
                curl = $"curl -X POST http://localhost:5000/api/auth/login -H \"Content-Type: application/json\" -d '{{\"username\":\"{request.Username}\",\"encryptedPassword\":\"{encryptedPassword}\"}}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密登录请求时发生错误");
            return StatusCode(500, "加密失败");
        }
    }
}