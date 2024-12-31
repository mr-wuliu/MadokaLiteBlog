using MadokaLiteBlog.Api.Models.DTO;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class UserMapper : BaseMapper<User>
{
    public UserMapper(NpgsqlConnection dbContext, ILogger<UserMapper> logger) : base(dbContext, logger)
    {

    }
}