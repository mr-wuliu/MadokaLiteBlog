using MadokaLiteBlog.Api.Mapper;
using MadokaLiteBlog.Api.Models;
using MadokaLiteBlog.Api.Models.VO;
namespace MadokaLiteBlog.Api.Service;

public class UserServer
{
    private readonly UserMapper _userMapper;
    private readonly ILogger _logger;
    public UserServer(UserMapper userMapper, ILogger<UserServer> logger)
    {
        _userMapper = userMapper;
        _logger = logger;
    }
    public async Task<UserVo> GetUserByUsernameAsync(string username)
    {
        var user = await _userMapper.GetByPropertyAsync(
            u => u.Username == username
        );
        if (user == null)
        {
            throw new Exception("用户不存在");
        }
        if (user.Count() == 0 || user.Count() > 1)
        {
            throw new Exception("数据异常");
        }
        var userVo = user.First();  
        return new UserVo
        {
            Id = userVo.Id,
            Username = userVo.Username,
            AvatarUrl = userVo.AvatarUrl,
            Motto = userVo.Motto,
            Email = userVo.Email
        };
    }
    public async Task<bool> ValidatePasswordAsync(string username, string password)
    {
        var user = await _userMapper.GetByPropertyAsync(
            u => u.Username == username,
            u => u.Password == password
        );
        if (user.Count() is 0 or > 1)
        {
            return false;
        }
        return true;
    }
    public async Task<long> RegisterUserAsync(RegisterRequest registerRequest)
    {
        var user = await _userMapper.GetByPropertyAsync(u => u.Username == registerRequest.Username);
        if (user.Count() > 0)
        {
            return 0;
        }
        var newUser = new User
        {
            Username = registerRequest.Username,
            Password = registerRequest.Password,
            Email = registerRequest.Email,
            AvatarUrl = registerRequest.AvatarUrl,
            Motto = registerRequest.Motto
        };
        var userId = await _userMapper.InsertAsync(newUser);
        return userId;
    }
}

