using MadokaLiteBlog.Api.Models;
using Npgsql;

namespace MadokaLiteBlog.Api.Data;

public class UserMapper : BaseMapper<User>
{
    public UserMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}