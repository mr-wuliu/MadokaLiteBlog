using Microsoft.AspNetCore.Mvc;
using MadokaLiteBlog.Api.Service;
using MadokaLiteBlog.Api.Models.VO;
using Microsoft.AspNetCore.Authorization;
using MadokaLiteBlog.Api.Extensions;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly UserServer _userService;
    private readonly ILogger<UserController> _logger;
    public UserController(UserServer userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    [Authorize]
    [HttpPost("info")]
    public async Task<IActionResult> Info()
    {
        var userId = User.GetUserId();
        if (userId <= 0)
        {
            return BadRequest("用户未登录");
        }
        var user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }
    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UserVo userVo)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId <= 0)
        {
            return BadRequest("用户未登录");
        }
        if (userVo.Id == null || userVo.Id <= 0)
        {
            return BadRequest("用户ID不可控");
        }
        var result = await _userService.UpdateUserAsync(userVo, currentUserId);
        return Ok(result);
    }
    [Authorize]
    [HttpPost("update-password")]
    public async Task<IActionResult> UpdatePassword([FromBody] string password)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId <= 0)
        {
            return BadRequest("用户未登录");
        }
        var result = await _userService.UpdateUserPasswordAsync(currentUserId, password);
        return Ok(result);
    }
}   

