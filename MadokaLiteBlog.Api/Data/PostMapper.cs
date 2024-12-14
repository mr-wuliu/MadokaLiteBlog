using MadokaLiteBlog.Api.Models;
using Npgsql;
namespace MadokaLiteBlog.Api.Data;

public class PostMapper : BaseMapper<Post>
{
    public PostMapper(NpgsqlConnection dbContext) : base(dbContext)
    {

    }
}