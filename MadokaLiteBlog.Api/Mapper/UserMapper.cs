using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Mapper;

public class UserMapper : BaseMapper<User>
{
    public UserMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}