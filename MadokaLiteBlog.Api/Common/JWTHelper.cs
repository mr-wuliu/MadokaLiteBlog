using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MadokaLiteBlog.Api.Service;

namespace MadokaLiteBlog.Api.Common;

public class JwtHelper
{
    private readonly IConfiguration _configuration;
    private readonly UserServer _userService;
    public JwtHelper(IConfiguration configuration, UserServer userService)
    {
        _configuration = configuration;
        _userService = userService;
    }
    public async Task<string> GenerateJwtToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var user = await _userService.GetUserByUsernameAsync(username) ?? throw new Exception("用户不存在");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString() ?? "0", ClaimValueTypes.UInteger64),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "1440")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}