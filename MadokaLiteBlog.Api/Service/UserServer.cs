using MadokaLiteBlog.Api.Mapper;

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
}
