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
    public async Task<List<UserVo>> GetUserByUsername(string username)
    {
        var users = await _userMapper.GetByPropertyAsync(
            u => u.Username == username
        );
        if (users == null)
        {
            throw new Exception("用户不存在");
        }
        return users.Select(u => new UserVo {
            Id = u.Id,
            Username = u.Username
        }).ToList();
    }
}
