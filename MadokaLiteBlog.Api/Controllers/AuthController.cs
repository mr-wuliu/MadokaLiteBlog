using MadokaLiteBlog.Api.Models.VO;
using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Service;
using MadokaLiteBlog.Api.Extensions;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    // 添加用户服务接口
    private readonly UserServer _userService;
    private readonly JwtHelper _jwtHelper;
    private readonly IWebHostEnvironment _environment;
    private readonly RsaHelper _rsaHelper;
    public AuthController(
        IConfiguration configuration,
        ILogger<AuthController> logger,
        UserServer userService,
        JwtHelper jwtHelper,
        IWebHostEnvironment environment,
        RsaHelper rsaHelper)
    {
        _configuration = configuration;
        _logger = logger;
        _userService = userService;
        _jwtHelper = jwtHelper;
        _environment = environment;
        _rsaHelper = rsaHelper;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        _logger.LogInformation("用户 {} 尝试登录", loginRequest.Username);
        _logger.LogInformation("密码: {}", loginRequest.Password);
        if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
        {
            return BadRequest("用户名或密码不能为空");
        }
        var privateKey = _configuration["Rsa:PrivateKey"];
        if (string.IsNullOrEmpty(privateKey))
        {
            return BadRequest("私钥不存在");
        }
        var password = RsaHelper.Decrypt(loginRequest.Password, privateKey);
        if (!await _userService.ValidatePasswordAsync(loginRequest.Username, password))
        {
            return BadRequest("用户名或密码错误");
        }
        _logger.LogInformation("用户 {} 登录成功", loginRequest.Username);
        
        var token = _jwtHelper.GenerateJwtToken(loginRequest.Username);
        return Ok(new LoginResponse { 
            Token = token,
            Username = loginRequest.Username 
        });
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        if (string.IsNullOrEmpty(registerRequest.Username) || string.IsNullOrEmpty(registerRequest.Password))
        {
            return BadRequest("用户名或密码不能为空");
        }
        var userId = await _userService.RegisterUserAsync(registerRequest);
        if (userId == 0)
        {
            return BadRequest("用户名已存在");
        }
        return Ok(userId);
    }
    [Authorize]
    [HttpPost("info")]
    public async Task<IActionResult> Info()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("用户未登录");
        }
        var user = await _userService.GetUserByUsernameAsync(username);
        return Ok(user);
    }
    [HttpPost("get-encrypt-password")]
    public IActionResult GetEncryptPassword(string password)
    {
        var publicKey = _configuration["Rsa:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
        {
            return BadRequest("公钥不存在");
        }
        return Ok(RsaHelper.Encrypt(password, publicKey));
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

    [HttpPost("generate-keys")]
    public async Task<IActionResult> GenerateKeys()
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("生产环境尝试生成密钥");
            return NotFound();
        }

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
}